namespace Centipede
{
    public class NullLogger : ILogger
    {
        public void Log(string message)
        {
            return;
        }
    }
}
