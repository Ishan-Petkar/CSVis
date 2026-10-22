namespace CSVis.Core
{
    /// <summary>Outcome of trying to turn a user's CSV into a plot.</summary>
    public struct PlotResult
    {
        public bool Success;

        /// <summary>Human readable explanation: the reason for failure, or what was plotted.</summary>
        public string Message;

        public int Points;

        public int SkippedRows;

        public static PlotResult Fail(string message)
        {
            return new PlotResult { Success = false, Message = message };
        }

        public static PlotResult Ok(string message, int points, int skippedRows = 0)
        {
            return new PlotResult { Success = true, Message = message, Points = points, SkippedRows = skippedRows };
        }
    }
}
