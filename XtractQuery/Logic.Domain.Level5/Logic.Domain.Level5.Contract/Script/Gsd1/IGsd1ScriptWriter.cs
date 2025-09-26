using Logic.Domain.Level5.Contract.Script.Gsd1.DataClasses;

namespace Logic.Domain.Level5.Contract.Script.Gsd1
{
    public interface IGsd1ScriptWriter
    {
        void Write(Gsd1ScriptFile script, Stream output);

        void Write(Gsd1ScriptContainer container, Stream output);
    }
}
