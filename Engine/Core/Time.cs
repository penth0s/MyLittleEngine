using OpenTK.Windowing.Common;

namespace Engine.Core;

/// <summary>
/// Provides time-related information for the current frame
/// </summary>
public static class Time
{
    #region Fields

    private static float _totalTime;

    #endregion

    #region Properties

    /// <summary>
    /// Frame event arguments containing timing information
    /// </summary>
    public static FrameEventArgs FrameEvent
    {
        internal set
        {
            // Update delta first
            _deltaTime = (float)value.Time;

            // Accumulate total time
            _totalTime += _deltaTime;

            _frameEvent = value;
        }
        get => _frameEvent;
    }

    private static FrameEventArgs _frameEvent;

    private static float _deltaTime;

    /// <summary>
    /// Time in seconds it took to complete the last frame (delta time)
    /// </summary>
    public static float DeltaTime => _deltaTime;

    /// <summary>
    /// Total time in seconds since the application started
    /// </summary>
    public static float TotalTime => _totalTime;

    #endregion
}