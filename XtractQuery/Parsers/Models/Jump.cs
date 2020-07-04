namespace XtractQuery.Parsers.Models
{
    class Jump
    {
        public string Label { get; }

        public Instruction Instruction { get; }

        public Jump(string label, Instruction instruction)
        {
            Label = label;
            Instruction = instruction;
        }

        public string GetString()
        {
            return Label;
        }
    }
}
