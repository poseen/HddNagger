namespace HddNagger
{
    /// <summary>
    /// Represents the state of the tasks.
    /// </summary>
	public enum TaskState
    {
        /// <summary>
        /// The service is working.
        /// </summary>
        Working,

        /// <summary>
        /// The service is paused.
        /// </summary>
        Paused,

        /// <summary>
        /// The service has received a stop signal and is currently stopping.
        /// </summary>
        Stopping
    }
}
