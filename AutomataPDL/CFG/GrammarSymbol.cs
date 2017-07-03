using System;


namespace AutomataPDL.CFG
{
    public abstract class GrammarSymbol
    {
        public abstract string Name { get; }

        public override string ToString()
        {
            return Name;
        }
    }
}
