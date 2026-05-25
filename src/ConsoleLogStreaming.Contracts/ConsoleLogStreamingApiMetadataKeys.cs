namespace ConsoleLogStreaming.Contracts;

/// <summary>
/// Well-known metadata keys used by ConsoleLogStreaming API DTO projection.
/// </summary>
public static class ConsoleLogStreamingApiMetadataKeys
{
    /// <summary>
    /// Kubernetes pod name.
    /// </summary>
    public const string KubernetesPodName = "kubernetes.pod.name";

    /// <summary>
    /// Container name.
    /// </summary>
    public const string ContainerName = "container.name";

    /// <summary>
    /// Kubernetes namespace.
    /// </summary>
    public const string KubernetesNamespace = "kubernetes.namespace";

    /// <summary>
    /// Kubernetes node name.
    /// </summary>
    public const string KubernetesNodeName = "kubernetes.node.name";

    /// <summary>
    /// Process start timestamp.
    /// </summary>
    public const string ProcessStartedAt = "process.startedAt";
}
