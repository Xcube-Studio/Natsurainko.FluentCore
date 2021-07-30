namespace FluentCore.Model
{
    public class JavaAgentModel
    {
        public string AgentPath { get; set; }

        public string Parameter { get; set; }

        public string ToArgument()
        {
            return $"-javaagent:"
                + (AgentPath.Contains(" ") ? $"\"{AgentPath}\"" : AgentPath)
                + $"={Parameter}";
        }
    }
}
