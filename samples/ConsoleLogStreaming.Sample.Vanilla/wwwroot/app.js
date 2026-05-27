const recordSeparator = String.fromCharCode(0x1e);
const recentPath = "/diagnostics/console-logs/recent";
const sourcesPath = "/diagnostics/console-logs/sources";
const hubPath = "/hubs/console-logs";
const maxVisibleLines = 500;

const elements = {
  statusLight: document.querySelector("#status-light"),
  connectionStatus: document.querySelector("#connection-status"),
  connectionDetail: document.querySelector("#connection-detail"),
  liveMetric: document.querySelector("#metric-live"),
  errorMetric: document.querySelector("#metric-errors"),
  sourceMetric: document.querySelector("#metric-sources"),
  sourceFilter: document.querySelector("#source-filter"),
  queryFilter: document.querySelector("#query-filter"),
  streamFilters: [...document.querySelectorAll("input[name='stream-filter']")],
  demoButtons: [...document.querySelectorAll("[data-demo]")],
  refreshRecent: document.querySelector("#refresh-recent"),
  clearLines: document.querySelector("#clear-lines"),
  autoScroll: document.querySelector("#auto-scroll"),
  backfillSummary: document.querySelector("#backfill-summary"),
  logViewport: document.querySelector("#log-viewport"),
  logList: document.querySelector("#log-list"),
  emptyState: document.querySelector("#empty-state")
};

const state = {
  socket: null,
  awaitingHandshake: true,
  reconnectTimer: 0,
  invocationId: 0,
  lines: [],
  liveLines: 0,
  errorLines: 0,
  droppedLines: 0,
  sources: new Map(),
  filterTimer: 0
};

connect();
loadSources();
loadRecent();

elements.sourceFilter.addEventListener("change", onFilterChanged);
elements.queryFilter.addEventListener("input", () => {
  window.clearTimeout(state.filterTimer);
  state.filterTimer = window.setTimeout(onFilterChanged, 220);
});

for (const filter of elements.streamFilters) {
  filter.addEventListener("change", onFilterChanged);
}

elements.refreshRecent.addEventListener("click", loadRecent);
elements.clearLines.addEventListener("click", () => {
  state.lines = [];
  renderLines();
  updateBackfillSummary("Visible lines cleared. Live streaming is still active.");
});

for (const button of elements.demoButtons) {
  button.addEventListener("click", () => writeDemoLines(button));
}

function connect() {
  const protocol = window.location.protocol === "https:" ? "wss:" : "ws:";
  const socket = new WebSocket(`${protocol}//${window.location.host}${hubPath}`);

  state.socket = socket;
  state.awaitingHandshake = true;
  setConnectionState("connecting", "Connecting", "Opening SignalR websocket");

  socket.addEventListener("open", () => {
    socket.send(JSON.stringify({ protocol: "json", version: 1 }) + recordSeparator);
  });

  socket.addEventListener("message", event => handleSocketMessage(String(event.data)));

  socket.addEventListener("close", () => {
    if (state.socket !== socket) {
      return;
    }

    setConnectionState("disconnected", "Disconnected", "Reconnecting shortly");
    state.reconnectTimer = window.setTimeout(connect, 1400);
  });

  socket.addEventListener("error", () => {
    setConnectionState("disconnected", "Connection error", "Websocket failed; waiting to retry");
  });
}

function handleSocketMessage(payload) {
  const messages = payload.split(recordSeparator).filter(Boolean);

  for (const raw of messages) {
    const message = JSON.parse(raw);

    if (state.awaitingHandshake) {
      state.awaitingHandshake = false;
      if (message.error) {
        setConnectionState("disconnected", "Handshake failed", message.error);
        return;
      }

      setConnectionState("connected", "Live", "SignalR hub connected");
      sendHubInvocation("SubscribeAsync", [buildFilter()]);
      continue;
    }

    handleHubMessage(message);
  }
}

function handleHubMessage(message) {
  if (message.type === 1 && message.target === "ReceiveConsoleLogLineAsync") {
    addLine(normalizeLine(message.arguments?.[0]), true);
    return;
  }

  if (message.type === 1 && message.target === "ReceiveDroppedLinesAsync") {
    const dropped = normalizeDropped(message.arguments?.[0]);
    state.droppedLines += dropped.count;
    addLine({
      id: `dropped-${Date.now()}`,
      timestamp: new Date().toISOString(),
      stream: "dropped",
      source: { displayName: dropped.sourceId || "provider" },
      text: `${dropped.count} line(s) were dropped by the bounded queue.`
    }, true);
    return;
  }

  if (message.type === 1 && message.target === "ReceiveSourceChangedAsync") {
    upsertSource(message.arguments?.[0]);
    renderSources();
  }
}

function sendHubInvocation(target, args) {
  if (!state.socket || state.socket.readyState !== WebSocket.OPEN || state.awaitingHandshake) {
    return;
  }

  state.socket.send(JSON.stringify({
    type: 1,
    invocationId: String(++state.invocationId),
    target,
    arguments: args
  }) + recordSeparator);
}

async function loadRecent() {
  const query = new URLSearchParams({ limit: "80" });
  const filter = buildFilter();

  if (filter.sourceId) {
    query.set("sourceId", filter.sourceId);
  }

  if (filter.query) {
    query.set("query", filter.query);
  }

  if (filter.stream === 0) {
    query.set("stream", "Stdout");
  } else if (filter.stream === 1) {
    query.set("stream", "Stderr");
  }

  elements.logViewport.setAttribute("aria-busy", "true");
  updateBackfillSummary("Loading recent lines");

  const response = await fetch(`${recentPath}?${query}`);
  const result = await response.json();
  const items = result.items ?? result.Items ?? [];
  const dropped = result.dropped ?? result.Dropped ?? [];
  const sources = result.sources ?? result.Sources ?? [];

  for (const source of sources) {
    upsertSource(source);
  }

  state.lines = items.map(normalizeLine);
  state.droppedLines = dropped.reduce((total, item) => total + normalizeDropped(item).count, 0);
  renderSources();
  renderLines();
  updateBackfillSummary(`Backfilled ${items.length} recent line${items.length === 1 ? "" : "s"}`);
  elements.logViewport.setAttribute("aria-busy", "false");
}

async function loadSources() {
  const response = await fetch(sourcesPath);
  const sources = await response.json();

  for (const source of sources) {
    upsertSource(source);
  }

  renderSources();
}

async function writeDemoLines(button) {
  const kind = button.dataset.demo;
  button.disabled = true;

  try {
    await fetch(`/demo/${kind}`, { method: "POST" });
  } finally {
    button.disabled = false;
  }
}

function onFilterChanged() {
  loadRecent();
  sendHubInvocation("UpdateFilterAsync", [buildFilter()]);
}

function buildFilter() {
  const sourceId = elements.sourceFilter.value;
  const query = elements.queryFilter.value.trim();
  const selectedStream = elements.streamFilters.find(item => item.checked)?.value ?? "";
  const filter = {};

  if (sourceId) {
    filter.sourceId = sourceId;
  }

  if (query) {
    filter.query = query;
  }

  if (selectedStream !== "") {
    filter.stream = Number(selectedStream);
  }

  return filter;
}

function normalizeLine(line) {
  const source = line.source ?? line.Source ?? {};
  return {
    id: line.id ?? line.Id ?? crypto.randomUUID(),
    timestamp: line.timestamp ?? line.Timestamp ?? line.receivedAt ?? line.ReceivedAt ?? new Date().toISOString(),
    stream: normalizeStream(line.stream ?? line.Stream),
    source: {
      id: source.id ?? source.Id ?? "",
      displayName: source.displayName ?? source.DisplayName ?? source.id ?? source.Id ?? "sample"
    },
    text: line.text ?? line.Text ?? ""
  };
}

function normalizeDropped(summary) {
  return {
    count: summary.count ?? summary.Count ?? 0,
    sourceId: summary.sourceId ?? summary.SourceId ?? ""
  };
}

function normalizeStream(stream) {
  if (stream === 1 || stream === "Stderr" || stream === "stderr") {
    return "stderr";
  }

  if (stream === "dropped") {
    return "dropped";
  }

  return "stdout";
}

function upsertSource(source) {
  const id = source.id ?? source.Id;

  if (!id) {
    return;
  }

  state.sources.set(id, {
    id,
    displayName: source.displayName ?? source.DisplayName ?? id
  });
}

function renderSources() {
  const selected = elements.sourceFilter.value;
  const sources = [...state.sources.values()].sort((left, right) => left.displayName.localeCompare(right.displayName));

  elements.sourceFilter.replaceChildren(new Option("All sources", ""));

  for (const source of sources) {
    elements.sourceFilter.append(new Option(source.displayName, source.id));
  }

  if (sources.some(source => source.id === selected)) {
    elements.sourceFilter.value = selected;
  }

  elements.sourceMetric.textContent = String(sources.length);
}

function addLine(line, isLive) {
  state.lines.push(line);

  if (state.lines.length > maxVisibleLines) {
    state.lines.splice(0, state.lines.length - maxVisibleLines);
  }

  if (isLive) {
    state.liveLines += 1;
  }

  if (line.stream === "stderr") {
    state.errorLines += 1;
  }

  renderLines();
}

function renderLines() {
  const fragment = document.createDocumentFragment();

  for (const line of state.lines) {
    const row = document.createElement("li");
    row.className = `log-row ${line.stream}`;

    row.append(
      createCell("log-time", formatTime(line.timestamp)),
      createCell("log-stream", line.stream),
      createCell("log-source", line.source.displayName),
      createCell("log-text", line.text)
    );

    fragment.append(row);
  }

  elements.logList.replaceChildren(fragment);
  elements.emptyState.classList.toggle("hidden", state.lines.length > 0);
  elements.liveMetric.textContent = String(state.liveLines);
  elements.errorMetric.textContent = String(state.errorLines);

  if (elements.autoScroll.checked) {
    elements.logViewport.scrollTop = elements.logViewport.scrollHeight;
  }
}

function createCell(className, text) {
  const element = document.createElement("span");
  element.className = className;
  element.textContent = text;
  return element;
}

function formatTime(value) {
  return new Intl.DateTimeFormat([], {
    hour: "2-digit",
    minute: "2-digit",
    second: "2-digit"
  }).format(new Date(value));
}

function setConnectionState(stateName, status, detail) {
  elements.statusLight.classList.toggle("connected", stateName === "connected");
  elements.statusLight.classList.toggle("disconnected", stateName === "disconnected");
  elements.connectionStatus.textContent = status;
  elements.connectionDetail.textContent = detail;
}

function updateBackfillSummary(text) {
  elements.backfillSummary.textContent = text;
}
