(function () {
  "use strict";

  const h = React.createElement;
  const { useCallback, useEffect, useMemo, useRef, useState } = React;
  const recentEndpoint = "/diagnostics/console-logs/recent";
  const sourcesEndpoint = "/diagnostics/console-logs/sources";
  const hubEndpoint = "/hubs/console-logs";
  const maxRows = 300;

  const streamOptions = [
    { label: "All", value: "all", apiValue: null },
    { label: "STDOUT", value: "stdout", apiValue: 0 },
    { label: "STDERR", value: "stderr", apiValue: 1 }
  ];

  function App() {
    const [status, setStatus] = useState("connecting");
    const [statusDetail, setStatusDetail] = useState("Opening SignalR connection");
    const [rows, setRows] = useState([]);
    const [sources, setSources] = useState([]);
    const [dropped, setDropped] = useState([]);
    const [query, setQuery] = useState("");
    const [stream, setStream] = useState("all");
    const [sourceId, setSourceId] = useState("");
    const [limit, setLimit] = useState(80);
    const [isLoading, setIsLoading] = useState(false);
    const [actionState, setActionState] = useState("Idle");
    const connectionRef = useRef(null);

    const filter = useMemo(
      () => buildFilter({ query, stream, sourceId, limit }),
      [query, stream, sourceId, limit]
    );
    const filterRef = useRef(filter);

    useEffect(() => {
      filterRef.current = filter;
    }, [filter]);

    const mergeRows = useCallback((incomingRows) => {
      setRows((currentRows) => {
        const merged = new Map();

        for (const row of incomingRows.map(normalizeLine)) {
          merged.set(row.id, row);
        }

        for (const row of currentRows) {
          if (!merged.has(row.id)) {
            merged.set(row.id, row);
          }
        }

        return Array.from(merged.values())
          .sort(compareRowsDescending)
          .slice(0, maxRows);
      });
    }, []);

    const loadSources = useCallback(async () => {
      const response = await fetch(sourcesEndpoint);
      if (!response.ok) {
        throw new Error(`Sources request failed with HTTP ${response.status}`);
      }

      setSources((await response.json()).map(normalizeSource));
    }, []);

    const loadRecent = useCallback(async () => {
      setIsLoading(true);

      try {
        const response = await fetch(`${recentEndpoint}?${toQueryString(filterRef.current)}`);
        if (!response.ok) {
          throw new Error(`Recent request failed with HTTP ${response.status}`);
        }

        const payload = await response.json();
        setRows((payload.items || []).map(normalizeLine).sort(compareRowsDescending));
        setDropped((payload.dropped || []).map(normalizeDropped));

        if (payload.sources) {
          setSources(payload.sources.map(normalizeSource));
        } else {
          await loadSources();
        }
      } catch (error) {
        setStatusDetail(error.message);
      } finally {
        setIsLoading(false);
      }
    }, [loadSources]);

    useEffect(() => {
      loadRecent();
    }, [loadRecent]);

    useEffect(() => {
      const timer = window.setTimeout(() => {
        loadRecent();

        const connection = connectionRef.current;
        if (connection && connection.state === signalR.HubConnectionState.Connected) {
          connection.invoke("UpdateFilterAsync", filterRef.current).catch((error) => {
            setStatusDetail(error.message);
          });
        }
      }, 200);

      return () => window.clearTimeout(timer);
    }, [filter, loadRecent]);

    useEffect(() => {
      const connection = new signalR.HubConnectionBuilder()
        .withUrl(hubEndpoint)
        .withAutomaticReconnect()
        .configureLogging(signalR.LogLevel.Warning)
        .build();

      connectionRef.current = connection;

      const onLine = (line) => mergeRows([line]);
      const onDropped = (summary) => {
        setDropped((current) => [normalizeDropped(summary), ...current].slice(0, 8));
      };
      const onSource = (source) => {
        setSources((current) => upsertSource(current, normalizeSource(source)));
      };

      connection.on("ReceiveConsoleLogLineAsync", onLine);
      connection.on("ReceiveConsoleLogLine", onLine);
      connection.on("ReceiveDroppedLinesAsync", onDropped);
      connection.on("ReceiveDroppedLines", onDropped);
      connection.on("ReceiveSourceChangedAsync", onSource);
      connection.on("ReceiveSourceChanged", onSource);

      connection.onreconnecting(() => {
        setStatus("reconnecting");
        setStatusDetail("SignalR reconnecting");
      });

      connection.onreconnected(async () => {
        setStatus("connected");
        setStatusDetail("Live stream resumed");
        await subscribe(connection, filterRef.current, setStatusDetail);
      });

      connection.onclose((error) => {
        setStatus("disconnected");
        setStatusDetail(error ? error.message : "SignalR connection closed");
      });

      connection.start()
        .then(async () => {
          setStatus("connected");
          setStatusDetail("Live stream connected");
          await subscribe(connection, filterRef.current, setStatusDetail);
        })
        .catch((error) => {
          setStatus("disconnected");
          setStatusDetail(error.message);
        });

      return () => {
        connectionRef.current = null;
        connection.stop();
      };
    }, [mergeRows]);

    const writeDemo = useCallback(async (kind) => {
      setActionState("Writing...");

      try {
        const response = await fetch(`/demo/${kind}`, { method: "POST" });
        if (!response.ok) {
          throw new Error(`Write request failed with HTTP ${response.status}`);
        }

        const result = await response.json();
        setActionState(`${result.linesWritten} ${result.stream} line${result.linesWritten === 1 ? "" : "s"} written`);
        window.setTimeout(loadRecent, 250);
      } catch (error) {
        setActionState(error.message);
      }
    }, [loadRecent]);

    const selectedSource = sourceId
      ? sources.find((source) => source.id === sourceId)
      : null;

    return h("main", { className: "shell" },
      h("section", { className: "masthead" },
        h("div", null,
          h("p", { className: "eyebrow" }, "Console Log Streaming"),
          h("h1", null, "React live console"),
          h("p", { className: "subtitle" }, "Backfilled and live stdout/stderr from the ASP.NET Core backend.")
        ),
        h(StatusPill, { status, detail: statusDetail })
      ),
      h("section", { className: "toolbar", "aria-label": "Console log controls" },
        h("label", { className: "field search-field" },
          h("span", null, "Query"),
          h("input", {
            value: query,
            placeholder: "Filter line text",
            onChange: (event) => setQuery(event.target.value)
          })
        ),
        h("label", { className: "field" },
          h("span", null, "Stream"),
          h("select", {
            value: stream,
            onChange: (event) => setStream(event.target.value)
          }, streamOptions.map((option) =>
            h("option", { key: option.value, value: option.value }, option.label)
          ))
        ),
        h("label", { className: "field" },
          h("span", null, "Source"),
          h("select", {
            value: sourceId,
            onChange: (event) => setSourceId(event.target.value)
          },
          h("option", { value: "" }, "All sources"),
          sources.map((source) =>
            h("option", { key: source.id, value: source.id }, source.displayName || source.id)
          ))
        ),
        h("label", { className: "field short-field" },
          h("span", null, "Backfill"),
          h("input", {
            type: "number",
            min: "10",
            max: "300",
            step: "10",
            value: limit,
            onChange: (event) => setLimit(clampLimit(event.target.value))
          })
        ),
        h("button", { className: "button ghost", onClick: loadRecent, disabled: isLoading },
          isLoading ? "Refreshing" : "Refresh"
        )
      ),
      h("section", { className: "metrics", "aria-label": "Console stream summary" },
        h(Metric, { label: "Rows", value: rows.length, detail: `${limit} requested` }),
        h(Metric, { label: "Sources", value: sources.length, detail: selectedSource ? selectedSource.health : "All active" }),
        h(Metric, { label: "Dropped", value: sumDropped(dropped), detail: dropped.length ? "Recent summaries" : "None reported" })
      ),
      h("section", { className: "actions", "aria-label": "Write demo console lines" },
        h("div", null,
          h("h2", null, "Demo writes"),
          h("p", null, actionState)
        ),
        h("div", { className: "button-row" },
          h("button", { className: "button primary", onClick: () => writeDemo("stdout") }, "Write stdout"),
          h("button", { className: "button danger", onClick: () => writeDemo("stderr") }, "Write stderr"),
          h("button", { className: "button dark", onClick: () => writeDemo("burst") }, "Backend burst")
        )
      ),
      h(LogTable, { rows }),
      dropped.length > 0 && h(DroppedPanel, { dropped })
    );
  }

  function StatusPill({ status, detail }) {
    return h("div", { className: `status status-${status}` },
      h("span", { className: "status-dot", "aria-hidden": "true" }),
      h("div", null,
        h("strong", null, status),
        h("small", null, detail)
      )
    );
  }

  function Metric({ label, value, detail }) {
    return h("article", { className: "metric" },
      h("span", null, label),
      h("strong", null, value),
      h("small", null, detail)
    );
  }

  function LogTable({ rows }) {
    return h("section", { className: "log-panel", "aria-label": "Console log rows" },
      h("div", { className: "log-header" },
        h("h2", null, "Console stream"),
        h("span", null, `${rows.length} rows`)
      ),
      h("div", { className: "table-wrap" },
        h("table", null,
          h("thead", null,
            h("tr", null,
              h("th", null, "Time"),
              h("th", null, "Stream"),
              h("th", null, "Source"),
              h("th", null, "Message")
            )
          ),
          h("tbody", null,
            rows.length === 0
              ? h("tr", null, h("td", { className: "empty", colSpan: "4" }, "No console lines match the current filter."))
              : rows.map((row) => h(LogRow, { key: row.id, row }))
          )
        )
      )
    );
  }

  function LogRow({ row }) {
    return h("tr", { className: `row-${row.streamClass}` },
      h("td", null, h("time", { dateTime: row.receivedAt }, formatTime(row.receivedAt))),
      h("td", null, h("span", { className: `stream-chip ${row.streamClass}` }, row.streamLabel)),
      h("td", null, h("span", { className: "source-name" }, row.source.displayName || row.source.id || "unknown")),
      h("td", { className: "message" }, row.text)
    );
  }

  function DroppedPanel({ dropped }) {
    return h("section", { className: "dropped-panel", "aria-label": "Dropped console lines" },
      h("h2", null, "Dropped summaries"),
      dropped.map((item, index) =>
        h("p", { key: `${item.reason}-${item.count}-${index}` },
          h("strong", null, item.count),
          ` ${item.streamLabel} line${item.count === 1 ? "" : "s"} dropped: ${item.reason}`
        )
      )
    );
  }

  async function subscribe(connection, currentFilter, setStatusDetail) {
    try {
      await connection.invoke("SubscribeAsync", currentFilter);
    } catch (error) {
      setStatusDetail(error.message);
    }
  }

  function buildFilter({ query, stream, sourceId, limit }) {
    const streamOption = streamOptions.find((option) => option.value === stream);
    const filter = { limit };

    if (query.trim()) {
      filter.query = query.trim();
    }

    if (sourceId) {
      filter.sourceId = sourceId;
    }

    if (streamOption && streamOption.apiValue !== null) {
      filter.stream = streamOption.apiValue;
    }

    return filter;
  }

  function toQueryString(filter) {
    const params = new URLSearchParams();

    if (filter.query) {
      params.set("query", filter.query);
    }

    if (filter.sourceId) {
      params.set("sourceId", filter.sourceId);
    }

    if (filter.stream !== undefined) {
      params.set("stream", String(filter.stream));
    }

    params.set("limit", String(filter.limit || 80));
    return params.toString();
  }

  function normalizeLine(line) {
    const stream = normalizeStream(line.stream);

    return {
      id: line.id || makeId(),
      receivedAt: line.receivedAt || line.timestamp || new Date().toISOString(),
      sequence: Number(line.sequence || 0),
      streamClass: stream.className,
      streamLabel: stream.label,
      text: line.text || "",
      source: normalizeSource(line.source || {})
    };
  }

  function normalizeDropped(summary) {
    const stream = normalizeStream(summary.stream);

    return {
      count: Number(summary.count || 0),
      reason: summary.reason || "bounded buffer",
      streamLabel: stream.label.toLowerCase()
    };
  }

  function normalizeSource(source) {
    return {
      id: source.id || "",
      displayName: source.displayName || source.id || "unknown",
      serviceName: source.serviceName || "",
      health: source.health || "Unknown",
      lastSeen: source.lastSeen || null
    };
  }

  function normalizeStream(value) {
    if (value === 1 || value === "Stderr" || value === "stderr") {
      return { label: "STDERR", className: "stderr" };
    }

    return { label: "STDOUT", className: "stdout" };
  }

  function upsertSource(current, source) {
    const next = current.filter((item) => item.id !== source.id);
    next.push(source);
    return next.sort((left, right) => left.displayName.localeCompare(right.displayName));
  }

  function compareRowsDescending(left, right) {
    const byTime = Date.parse(right.receivedAt) - Date.parse(left.receivedAt);
    return byTime || right.sequence - left.sequence;
  }

  function sumDropped(items) {
    return items.reduce((total, item) => total + item.count, 0);
  }

  function clampLimit(value) {
    const parsed = Number(value);

    if (Number.isNaN(parsed)) {
      return 80;
    }

    return Math.max(10, Math.min(300, parsed));
  }

  function formatTime(value) {
    return new Intl.DateTimeFormat(undefined, {
      hour: "2-digit",
      minute: "2-digit",
      second: "2-digit"
    }).format(new Date(value));
  }

  function makeId() {
    if (window.crypto && window.crypto.randomUUID) {
      return window.crypto.randomUUID();
    }

    return `${Date.now()}-${Math.random()}`;
  }

  ReactDOM.createRoot(document.getElementById("root")).render(h(App));
})();
