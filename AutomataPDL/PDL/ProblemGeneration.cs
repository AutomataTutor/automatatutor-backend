using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

using Microsoft.Automata;
using Microsoft.Z3;

namespace AutomataPDL
{
    public abstract class CPDLSet
    {
        protected string constraintVariable;

        public abstract IEnumerable<PDLSet> GetConcretizations(int distance, IEnumerable<char> alphabet);

        public abstract void ToSMTConstraints(Context z3Context, Solver z3Solver, int alphabetSize, VariableCache variableGenerator);

        public ICollection<string> GetChoiceVariables()
        {
            return this.CollectChoiceVariables(new HashSet<string>());
        }

        public abstract HashSet<string> CollectChoiceVariables(HashSet<string> currentVals);

        public abstract PDLSet InterpretModel(IList<char> alphabet, Context context, Model model);

        protected int GetConcChoice(Context context, Model model)
        {
            int concChoice = ((IntNum)model.ConstInterp(context.MkIntConst(this.constraintVariable))).Int;
            return concChoice;
        }
    }

    public abstract class CPDLPos
    {
        protected string constraintVariable;

        public abstract IEnumerable<PDLPos> GetConcretizations(int distance, IEnumerable<char> alphabet);

        public abstract void ToSMTConstraints(Context z3Context, Solver z3Solver, int alphabetSize, VariableCache variableGenerator);

        public ICollection<string> GetChoiceVariables()
        {
            return this.CollectChoiceVariables(new HashSet<string>());
        }

        public abstract HashSet<string> CollectChoiceVariables(HashSet<string> currentVals);

        public abstract PDLPos InterpretModel(IList<char> alphabet, Context context, Model model);

        protected int GetConcChoice(Context context, Model model)
        {
            int concChoice = ((IntNum)model.ConstInterp(context.MkIntConst(this.constraintVariable))).Int;
            return concChoice;
        }
    }

    public abstract class CPDLPred
    {
        protected string constraintVariable;

        public abstract IEnumerable<PDLPred> GetConcretizations(int distance, IEnumerable<char> alphabet);

        public abstract void ToSMTConstraints(Context z3Context, Solver z3Solver, int alphabetSize, VariableCache variableGenerator);

        public ICollection<string> GetChoiceVariables()
        {
            return this.CollectChoiceVariables(new HashSet<string>());
        }

        public abstract HashSet<string> CollectChoiceVariables(HashSet<string> currentVals);

        public abstract PDLPred InterpretModel(IList<char> alphabet, Context context, Model model);

        protected int GetConcChoice(Context context, Model model)
        {
            int concChoice = ((IntNum)model.ConstInterp(context.MkIntConst(this.constraintVariable))).Int;
            return concChoice;
        }
    }

    /// <summary>
    /// Generates the following formulas:
    ///     - phi1 AND phi2
    ///     - phi1 OR phi2
    /// if the original formula was either of these or
    ///     - IF phi1 THEN phi2
    ///     - phi1 IFF phi
    /// otherwise
    /// </summary>
    public class CPDLLogConnPred : CPDLPred
    {
        private readonly CPDLPred lhs, rhs;
        private readonly PDLLogicalOperator original;

        public CPDLLogConnPred(CPDLPred lhs, CPDLPred rhs, PDLLogicalOperator original) {
            this.lhs = lhs;
            this.rhs = rhs;
            this.original = original;
        }

        public override IEnumerable<PDLPred> GetConcretizations(int distance, IEnumerable<char> alphabet)
        {
            foreach (PDLPred lhsConc in this.lhs.GetConcretizations(distance, alphabet)) {
                foreach (PDLPred rhsConc in this.rhs.GetConcretizations(distance, alphabet))
                {
                    if (original.Equals(PDLLogicalOperator.And) || original.Equals(PDLLogicalOperator.Or))
                    {
                        yield return new PDLAnd(lhsConc, rhsConc);
                        yield return new PDLOr(lhsConc, rhsConc);
                    }
                    else
                    {
                        yield return new PDLIf(lhsConc, rhsConc);
                        yield return new PDLIff(lhsConc, rhsConc);
                    }
                }
            }
        }


        public override void ToSMTConstraints(Context z3Context, Solver z3Solver, int alphabetSize, VariableCache variableGenerator)
        {
            this.constraintVariable = variableGenerator.GetFreshVariableName();
            ArithExpr myVariable = z3Context.MkIntConst(this.constraintVariable);
            z3Solver.Assert(z3Context.MkLe(z3Context.MkInt(0), myVariable));
            z3Solver.Assert(z3Context.MkLe(myVariable, z3Context.MkInt(1)));
            this.lhs.ToSMTConstraints(z3Context, z3Solver, alphabetSize, variableGenerator);
            this.rhs.ToSMTConstraints(z3Context, z3Solver, alphabetSize, variableGenerator);
        }

        public override HashSet<string> CollectChoiceVariables(HashSet<string> currentVals)
        {
            currentVals.Add(this.constraintVariable);
            currentVals = this.lhs.CollectChoiceVariables(currentVals);
            return this.rhs.CollectChoiceVariables(currentVals);
        }

        public override PDLPred InterpretModel(IList<char> alphabet, Context context, Model model)
        {
            int concChoice = this.GetConcChoice(context, model);
            PDLPred lhsConc = this.lhs.InterpretModel(alphabet, context, model);
            PDLPred rhsConc = this.rhs.InterpretModel(alphabet, context, model);

            if (original.Equals(PDLLogicalOperator.And) || original.Equals(PDLLogicalOperator.Or))
            {
                if (concChoice == 0)
                {
                    return new PDLAnd(lhsConc, rhsConc);
                }
                else
                {
                    return new PDLOr(lhsConc, rhsConc);
                }
            }
            else
            {
                if (concChoice == 0)
                {
                    return new PDLIf(lhsConc, rhsConc);
                }
                else 
                {
                    return new PDLIff(lhsConc, rhsConc);
                }
            }
        }
    }

    /// <summary>
    /// Generates the following formulas
    ///     - FORALL x: phi
    ///     - EXISTS x: phi
    /// where both FORALL and EXISTS are First-order-quantifiers
    /// </summary>
    public class CPDLFirstOrderQuantifierPred : CPDLPred
    {
        private readonly string variableName;
        private readonly CPDLPred originalFormula;

        public CPDLFirstOrderQuantifierPred(string variableName, CPDLPred originalFormula)
        {
            Debug.Assert(variableName != null);
            Debug.Assert(originalFormula != null);
            this.variableName = variableName;
            this.originalFormula = originalFormula;
        }

        public override IEnumerable<PDLPred> GetConcretizations(int distance, IEnumerable<char> alphabet)
        {
            foreach(PDLPred originalConc in this.originalFormula.GetConcretizations(distance, alphabet)) {
                yield return new PDLForallFO(this.variableName, originalConc);
                yield return new PDLExistsFO(this.variableName, originalConc);
            }
        }

        public override void ToSMTConstraints(Context z3Context, Solver z3Solver, int alphabetSize, VariableCache variableGenerator)
        {
            this.constraintVariable = variableGenerator.GetFreshVariableName();
            ArithExpr myVariable = z3Context.MkIntConst(this.constraintVariable);
            z3Solver.Assert(z3Context.MkLe(z3Context.MkInt(0), myVariable));
            z3Solver.Assert(z3Context.MkLe(myVariable, z3Context.MkInt(1)));
            this.originalFormula.ToSMTConstraints(z3Context, z3Solver, alphabetSize, variableGenerator);
        }

        public override HashSet<string> CollectChoiceVariables(HashSet<string> currentVals)
        {
            currentVals.Add(this.constraintVariable);
            return this.originalFormula.CollectChoiceVariables(currentVals);
        }

        public override PDLPred InterpretModel(IList<char> alphabet, Context context, Model model)
        {
            int concChoice = this.GetConcChoice(context, model);
            PDLPred originalConc = this.originalFormula.InterpretModel(alphabet, context, model);
            switch (concChoice)
            {
                case 0: return new PDLForallFO(this.variableName, originalConc);
                case 1: return new PDLExistsFO(this.variableName, originalConc);
            }
            return null;
        }
    }

    /// <summary>
    /// Generates the following formulas
    ///     - FORALL x: phi
    ///     - EXISTS x: phi
    /// where both FORALL and EXISTS are Second-order-quantifiers
    /// </summary>
    public class CPDLSecondOrderQuantifierPred : CPDLPred
    {
        private readonly string variableName;
        private readonly CPDLPred originalFormula;

        public CPDLSecondOrderQuantifierPred(string variableName, CPDLPred originalFormula)
        {
            Debug.Assert(variableName != null);
            Debug.Assert(originalFormula != null);
            this.variableName = variableName;
            this.originalFormula = originalFormula;
        }

        public override IEnumerable<PDLPred> GetConcretizations(int distance, IEnumerable<char> alphabet)
        {
            foreach(PDLPred originalConc in this.originalFormula.GetConcretizations(distance, alphabet)) {
                yield return new PDLForallSO(this.variableName, originalConc);
                yield return new PDLExistsSO(this.variableName, originalConc);
            }
        }

        public override void ToSMTConstraints(Context z3Context, Solver z3Solver, int alphabetSize, VariableCache variableGenerator)
        {
            this.constraintVariable = variableGenerator.GetFreshVariableName();
            ArithExpr myVariable = z3Context.MkIntConst(this.constraintVariable);
            z3Solver.Assert(z3Context.MkLe(z3Context.MkInt(0), myVariable));
            z3Solver.Assert(z3Context.MkLe(myVariable, z3Context.MkInt(1)));
            this.originalFormula.ToSMTConstraints(z3Context, z3Solver, alphabetSize, variableGenerator);
        }

        public override HashSet<string> CollectChoiceVariables(HashSet<string> currentVals)
        {
            currentVals.Add(this.constraintVariable);
            return this.originalFormula.CollectChoiceVariables(currentVals);
        }

        public override PDLPred InterpretModel(IList<char> alphabet, Context context, Model model)
        {
            int concChoice = this.GetConcChoice(context, model);
            PDLPred originalConc = this.originalFormula.InterpretModel(alphabet, context, model);
            switch (concChoice)
            {
                case 0: return new PDLForallSO(this.variableName, originalConc);
                case 1: return new PDLExistsSO(this.variableName, originalConc);
            }
            return null;
        }
    }

    /// <summary>
    /// Generates the following formulas:
    ///     - phi
    ///     - NEG phi
    /// </summary>
    public class CPDLNegationPred : CPDLPred
    {
        private readonly CPDLPred original;

        public CPDLNegationPred(CPDLPred original)
        {
            this.original = original;
        }

        public override IEnumerable<PDLPred> GetConcretizations(int distance, IEnumerable<char> alphabet)
        {
            foreach (PDLPred origConc in original.GetConcretizations(distance, alphabet))
            {
                yield return origConc;
                yield return new PDLNot(origConc);
            }
        }

        public override void ToSMTConstraints(Context z3Context, Solver z3Solver, int alphabetSize, VariableCache variableGenerator)
        {
            this.constraintVariable = variableGenerator.GetFreshVariableName();
            ArithExpr myVariable = z3Context.MkIntConst(this.constraintVariable);
            z3Solver.Assert(z3Context.MkLe(z3Context.MkInt(0), myVariable));
            z3Solver.Assert(z3Context.MkLe(myVariable, z3Context.MkInt(1)));
            this.original.ToSMTConstraints(z3Context, z3Solver, alphabetSize, variableGenerator);
        }

        public override HashSet<string> CollectChoiceVariables(HashSet<string> currentVals)
        {
            currentVals.Add(this.constraintVariable);
            return this.original.CollectChoiceVariables(currentVals);
        }

        public override PDLPred InterpretModel(IList<char> alphabet, Context context, Model model)
        {
            int concChoice = this.GetConcChoice(context, model);
            PDLPred originalConc = this.original.InterpretModel(alphabet, context, model);
            switch (concChoice)
            {
                case 0: return originalConc;
                case 1: return new PDLNot(originalConc);
            }
            return null;
        }
    }

    /// <summary>
    /// Generates the following formulas:
    ///     - TRUE
    ///     - FALSE
    /// </summary>
    public class CPDLConstantPred : CPDLPred
    {
        public override IEnumerable<PDLPred> GetConcretizations(int distance, IEnumerable<char> alphabet)
        {
            yield return new PDLTrue();
            yield return new PDLFalse();
        }

        public override PDLPred InterpretModel(IList<char> alphabet, Context context, Model model)
        {
            int concChoice = this.GetConcChoice(context, model);
            switch (concChoice)
            {
                case 0: return new PDLTrue();
                case 1: return new PDLFalse();
            }
            return null;
        }

        public override void ToSMTConstraints(Context z3Context, Solver z3Solver, int alphabetSize, VariableCache variableGenerator)
        {
            this.constraintVariable = variableGenerator.GetFreshVariableName();
            ArithExpr myVariable = z3Context.MkIntConst(this.constraintVariable);
            z3Solver.Assert(z3Context.MkLe(z3Context.MkInt(0), myVariable));
            z3Solver.Assert(z3Context.MkLe(myVariable, z3Context.MkInt(1)));
        }

        public override HashSet<string> CollectChoiceVariables(HashSet<string> currentVals)
        {
            currentVals.Add(this.constraintVariable);
            return currentVals;
        }
    }

    /// <summary>
    /// Generates the following formulas:
    ///     - p1 COMP p2
    /// for all COMP in LEQ, LT, EQ, GT, GEQ
    /// </summary>
    public class CPDLBinaryPosPred : CPDLPred
    {
        private readonly CPDLPos lhs, rhs;

        public CPDLBinaryPosPred(CPDLPos lhs, CPDLPos rhs)
        {
            this.lhs = lhs;
            this.rhs = rhs;
        }

        public override IEnumerable<PDLPred> GetConcretizations(int distance, IEnumerable<char> alphabet)
        {
            foreach (PDLPos lhsConc in this.lhs.GetConcretizations(distance, alphabet))
            {
                foreach (PDLPos rhsConc in this.rhs.GetConcretizations(distance, alphabet))
                {
                    foreach (PDLPosComparisonOperator op in Enum.GetValues(typeof(PDLPosComparisonOperator)))
                    {
                        yield return new PDLBinaryPosFormula(lhsConc, rhsConc, op);
                    }
                }

            }
        }

        public override PDLPred InterpretModel(IList<char> alphabet, Context context, Model model)
        {
            int concChoice = this.GetConcChoice(context, model);
            PDLPos lhsConc = this.lhs.InterpretModel(alphabet, context, model);
            PDLPos rhsConc = this.rhs.InterpretModel(alphabet, context, model);
            switch (concChoice)
            {
                case 0: return new PDLBinaryPosFormula(lhsConc, rhsConc, PDLPosComparisonOperator.Eq);
                case 1: return new PDLBinaryPosFormula(lhsConc, rhsConc, PDLPosComparisonOperator.Ge);
                case 2: return new PDLBinaryPosFormula(lhsConc, rhsConc, PDLPosComparisonOperator.Geq);
                case 3: return new PDLBinaryPosFormula(lhsConc, rhsConc, PDLPosComparisonOperator.Le);
                case 4: return new PDLBinaryPosFormula(lhsConc, rhsConc, PDLPosComparisonOperator.Leq);
            }
            return null;
        }

        public override void ToSMTConstraints(Context z3Context, Solver z3Solver, int alphabetSize, VariableCache variableGenerator)
        {
            this.constraintVariable = variableGenerator.GetFreshVariableName();
            ArithExpr myVariable = z3Context.MkIntConst(this.constraintVariable);
            z3Solver.Assert(z3Context.MkLe(z3Context.MkInt(0), myVariable));
            z3Solver.Assert(z3Context.MkLe(myVariable, z3Context.MkInt(4)));
            this.lhs.ToSMTConstraints(z3Context, z3Solver, alphabetSize, variableGenerator);
            this.rhs.ToSMTConstraints(z3Context, z3Solver, alphabetSize, variableGenerator);
        }

        public override HashSet<string> CollectChoiceVariables(HashSet<string> currentVals)
        {
            currentVals.Add(this.constraintVariable);
            currentVals = this.lhs.CollectChoiceVariables(currentVals);
            return this.rhs.CollectChoiceVariables(currentVals);
        }
    }

    /// <summary>
    /// Generates the following formulas:
    ///     - a @ p1
    /// for all a in the alphabet
    /// </summary>
    public class CPDLAtPosPred : CPDLPred
    {
        private readonly CPDLPos originalPos;
        private readonly CPDLChar originalChar;

        public CPDLAtPosPred(CPDLChar originalChar, CPDLPos originalPos)
        {
            this.originalPos = originalPos;
            this.originalChar = originalChar;
        }

        public override IEnumerable<PDLPred> GetConcretizations(int distance, IEnumerable<char> alphabet)
        {
            foreach(PDLPos concPos in this.originalPos.GetConcretizations(distance, alphabet))
            {
                foreach (char symbol in alphabet)
                {
                    yield return new PDLAtPos(symbol, concPos);
                }
            }
        }

        public override PDLPred InterpretModel(IList<char> alphabet, Context context, Model model)
        {
            PDLPos posConc = this.originalPos.InterpretModel(alphabet, context, model);
            char charConc = this.originalChar.InterpretModel(alphabet, context, model);
            return new PDLAtPos(charConc, posConc);
        }

        public override void ToSMTConstraints(Context z3Context, Solver z3Solver, int alphabetSize, VariableCache variableGenerator)
        {
            this.constraintVariable = variableGenerator.GetFreshVariableName();
            ArithExpr myVariable = z3Context.MkIntConst(this.constraintVariable);
            z3Solver.Assert(z3Context.MkEq(myVariable, z3Context.MkInt(0)));
            this.originalPos.ToSMTConstraints(z3Context, z3Solver, alphabetSize, variableGenerator);
            this.originalChar.ToSMTConstraints(z3Context, z3Solver, alphabetSize, variableGenerator);
        }

        public override HashSet<string> CollectChoiceVariables(HashSet<string> currentVals)
        {
            currentVals.Add(this.constraintVariable);
            currentVals = this.originalChar.CollectChoiceVariables(currentVals);
            return this.originalPos.CollectChoiceVariables(currentVals);
        }
    }

    /// <summary>
    /// Generates the following formulas:
    ///     - a @ s1
    /// for all a in the alphabet
    /// </summary>
    public class CPDLAtSetPred : CPDLPred
    {
        private readonly CPDLSet originalSet;
        private readonly CPDLChar originalChar;

        public CPDLAtSetPred(CPDLChar originalChar, CPDLSet originalSet)
        {
            this.originalChar = originalChar;
            this.originalSet = originalSet;
        }

        public override IEnumerable<PDLPred> GetConcretizations(int distance, IEnumerable<char> alphabet)
        {
            foreach (PDLSet concSet in this.originalSet.GetConcretizations(distance, alphabet))
            {
                foreach (char symbol in alphabet)
                {
                    yield return new PDLAtSet(symbol, concSet);
                }
            }
        }

        public override PDLPred InterpretModel(IList<char> alphabet, Context context, Model model)
        {
            char charConc = this.originalChar.InterpretModel(alphabet, context, model);
            PDLSet posConc = this.originalSet.InterpretModel(alphabet, context, model);
            return new PDLAtSet(charConc, posConc);
        }

        public override void ToSMTConstraints(Context z3Context, Solver z3Solver, int alphabetSize, VariableCache variableGenerator)
        {
            this.constraintVariable = variableGenerator.GetFreshVariableName();
            ArithExpr myVariable = z3Context.MkIntConst(this.constraintVariable);
            z3Solver.Assert(z3Context.MkEq(myVariable, z3Context.MkInt(0)));
            this.originalSet.ToSMTConstraints(z3Context, z3Solver, alphabetSize, variableGenerator);
            this.originalChar.ToSMTConstraints(z3Context, z3Solver, alphabetSize, variableGenerator);
        }

        public override HashSet<string> CollectChoiceVariables(HashSet<string> currentVals)
        {
            currentVals.Add(this.constraintVariable);
            currentVals = this.originalChar.CollectChoiceVariables(currentVals);
            return this.originalSet.CollectChoiceVariables(currentVals);
        }
    }

    /// <summary>
    /// Generates the following formulas
    ///     - p1 IN s1
    /// </summary>
    public class CPDLBelongs : CPDLPred
    {
        private readonly CPDLPos originalPosition;
        private readonly CPDLSet originalSet;

        public CPDLBelongs(CPDLPos originalPosition, CPDLSet originalSet)
        {
            this.originalPosition = originalPosition;
            this.originalSet = originalSet;
        }

        public override IEnumerable<PDLPred> GetConcretizations(int distance, IEnumerable<char> alphabet)
        {
            foreach(PDLPos concPos in this.originalPosition.GetConcretizations(distance, alphabet)) {
                foreach(PDLSet concSet in this.originalSet.GetConcretizations(distance, alphabet)) {
                    yield return new PDLBelongs(concPos, concSet);
                }
            }
        }

        public override PDLPred InterpretModel(IList<char> alphabet, Context context, Model model)
        {
            PDLPos posConc = this.originalPosition.InterpretModel(alphabet, context, model);
            PDLSet setConc = this.originalSet.InterpretModel(alphabet, context, model);
            return new PDLBelongs(posConc, setConc);
        }

        public override void ToSMTConstraints(Context z3Context, Solver z3Solver, int alphabetSize, VariableCache variableGenerator)
        {
            this.constraintVariable = variableGenerator.GetFreshVariableName();
            ArithExpr myVariable = z3Context.MkIntConst(this.constraintVariable);
            z3Solver.Assert(z3Context.MkEq(myVariable, z3Context.MkInt(0)));
            this.originalPosition.ToSMTConstraints(z3Context, z3Solver, alphabetSize, variableGenerator);
            this.originalSet.ToSMTConstraints(z3Context, z3Solver, alphabetSize, variableGenerator);
        }

        public override HashSet<string> CollectChoiceVariables(HashSet<string> currentVals)
        {
            currentVals.Add(this.constraintVariable);
            currentVals = this.originalPosition.CollectChoiceVariables(currentVals);
            return this.originalSet.CollectChoiceVariables(currentVals);
        }
    }

    /// <summary>
    /// Generates the following formulas:
    ///     - |s| COMP n
    /// for all COMP in LEQ, LT, EQ, GT, GEQ, where n is dependent on the original n
    /// </summary>
    public class CPDLSetCardinality : CPDLPred
    {
        private readonly CPDLSet originalSet;
        private readonly CPDLInteger originalN;

        public CPDLSetCardinality(CPDLSet originalSet, CPDLInteger originalN)
        {
            this.originalSet = originalSet;
            this.originalN = originalN;
        }

        public override IEnumerable<PDLPred> GetConcretizations(int distance, IEnumerable<char> alphabet)
        {
            foreach(PDLSet concSet in this.originalSet.GetConcretizations(distance, alphabet)){
                foreach (int n in this.originalN.GetConcretizations(distance))
                {
                    foreach (PDLComparisonOperator op in Enum.GetValues(typeof(PDLComparisonOperator)))
                    {
                        yield return new PDLSetCardinality(concSet, n, op);
                    }
                }
            }
        }

        public override PDLPred InterpretModel(IList<char> alphabet, Context context, Model model)
        {
            int concChoice = this.GetConcChoice(context, model);
            PDLSet concSet = this.originalSet.InterpretModel(alphabet, context, model);
            int n = this.originalN.InterpretModel(alphabet, context, model);
            switch (concChoice)
            {
                case 0: return new PDLSetCardinality(concSet, n, PDLComparisonOperator.Eq);
                case 1: return new PDLSetCardinality(concSet, n, PDLComparisonOperator.Ge);
                case 2: return new PDLSetCardinality(concSet, n, PDLComparisonOperator.Geq);
                case 3: return new PDLSetCardinality(concSet, n, PDLComparisonOperator.Le);
                case 4: return new PDLSetCardinality(concSet, n, PDLComparisonOperator.Leq);
            }
            return null;
        }

        public override void ToSMTConstraints(Context z3Context, Solver z3Solver, int alphabetSize, VariableCache variableGenerator)
        {
            this.constraintVariable = variableGenerator.GetFreshVariableName();
            ArithExpr myVariable = z3Context.MkIntConst(this.constraintVariable);

            z3Solver.Assert(z3Context.MkLe(z3Context.MkInt(0), myVariable));
            z3Solver.Assert(z3Context.MkLe(myVariable, z3Context.MkInt(4)));

            this.originalN.ToSMTConstraints(z3Context, z3Solver, alphabetSize, variableGenerator);
            this.originalSet.ToSMTConstraints(z3Context, z3Solver, alphabetSize, variableGenerator);
        }

        public override HashSet<string> CollectChoiceVariables(HashSet<string> currentVals)
        {
            currentVals.Add(this.constraintVariable);
            currentVals = this.originalN.CollectChoiceVariables(currentVals);
            return this.originalSet.CollectChoiceVariables(currentVals);
        }
    }

    /// <summary>
    /// Generates the following formulas:
    ///     - |s| %m COMP n
    /// for all COMP in LEQ, LT, EQ, GT, GEQ, where n is dependent on the original n
    /// </summary>
    public class CPDLSetCardinalityModule : CPDLPred
    {
        private readonly CPDLSet originalSet;
        private readonly CPDLInteger originalN, originalM;

        public CPDLSetCardinalityModule(CPDLSet originalSet, CPDLInteger originalN, CPDLInteger originalM)
        {
            this.originalSet = originalSet;
            this.originalN = originalN;
            this.originalM = originalM;
        }

        public override IEnumerable<PDLPred> GetConcretizations(int distance, IEnumerable<char> alphabet)
        {
            foreach(PDLSet concSet in this.originalSet.GetConcretizations(distance, alphabet)){
                foreach (int n in this.originalN.GetConcretizations(distance))
                {
                    foreach (int m in this.originalM.GetConcretizations(distance))
                    {
                        foreach (PDLComparisonOperator op in Enum.GetValues(typeof(PDLComparisonOperator)))
                        {
                            yield return new PDLSetModuleComparison(concSet, n, m, op);
                        }
                    }
                }
            }
        }

        public override PDLPred InterpretModel(IList<char> alphabet, Context context, Model model)
        {
            int concChoice = this.GetConcChoice(context, model);
            PDLSet concSet = this.originalSet.InterpretModel(alphabet, context, model);
            int m = this.originalM.InterpretModel(alphabet, context, model);
            int n = this.originalN.InterpretModel(alphabet, context, model);
            switch (concChoice)
            {
                case 0: return new PDLSetModuleComparison(concSet, m, n, PDLComparisonOperator.Eq);
                case 1: return new PDLSetModuleComparison(concSet, m, n, PDLComparisonOperator.Ge);
                case 2: return new PDLSetModuleComparison(concSet, m, n, PDLComparisonOperator.Geq);
                case 3: return new PDLSetModuleComparison(concSet, m, n, PDLComparisonOperator.Le);
                case 4: return new PDLSetModuleComparison(concSet, m, n, PDLComparisonOperator.Leq);
            }
            return null;
        }

        public override void ToSMTConstraints(Context z3Context, Solver z3Solver, int alphabetSize, VariableCache variableGenerator)
        {
            this.constraintVariable = variableGenerator.GetFreshVariableName();
            ArithExpr myVariable = z3Context.MkIntConst(this.constraintVariable);

            z3Solver.Assert(z3Context.MkLe(z3Context.MkInt(0), myVariable));
            z3Solver.Assert(z3Context.MkLe(myVariable, z3Context.MkInt(4)));

            this.originalN.ToSMTConstraints(z3Context, z3Solver, alphabetSize, variableGenerator);
            this.originalM.ToSMTConstraints(z3Context, z3Solver, alphabetSize, variableGenerator);
            this.originalSet.ToSMTConstraints(z3Context, z3Solver, alphabetSize, variableGenerator);
        }

        public override HashSet<string> CollectChoiceVariables(HashSet<string> currentVals)
        {
            currentVals.Add(this.constraintVariable);
            currentVals = this.originalN.CollectChoiceVariables(currentVals);
            return this.originalSet.CollectChoiceVariables(currentVals);
        }
    }

    /// <summary>
    /// Generates the following formulas:
    ///     - begWt s1
    ///     - endWt s1
    /// </summary>
    public class CPDLBegEndWithPred : CPDLPred
    {
        private readonly CPDLString original;

        public CPDLBegEndWithPred(CPDLString original)
        {
            this.original = original;
        }

        public override IEnumerable<PDLPred> GetConcretizations(int distance, IEnumerable<char> alphabet)
        {
            foreach (string concString in original.GetConcretizations(distance, alphabet))
            {
                yield return new PDLStartsWith(concString);
                yield return new PDLEndsWith(concString);
            }
        }

        public override PDLPred InterpretModel(IList<char> alphabet, Context context, Model model)
        {
            int concChoice = this.GetConcChoice(context, model);
            string stringConc = this.original.InterpretModel(alphabet, context, model);
            if (concChoice == 0) { return new PDLStartsWith(stringConc); }
            else { return new PDLEndsWith(stringConc); }
        }

        public override void ToSMTConstraints(Context z3Context, Solver z3Solver, int alphabetSize, VariableCache variableGenerator)
        {
            this.constraintVariable = variableGenerator.GetFreshVariableName();
            ArithExpr myVariable = z3Context.MkIntConst(this.constraintVariable);
            z3Solver.Assert(z3Context.MkLe(z3Context.MkInt(0), myVariable));
            z3Solver.Assert(z3Context.MkLe(myVariable, z3Context.MkInt(1)));
            this.original.ToSMTConstraints(z3Context, z3Solver, alphabetSize, variableGenerator);
        }

        public override HashSet<string> CollectChoiceVariables(HashSet<string> currentVals)
        {
            currentVals.Add(this.constraintVariable);
            return this.original.CollectChoiceVariables(currentVals);
        }
    }

    /// <summary>
    /// Generates the following formulas:
    ///     - isEmpty
    /// </summary>
    public class CPDLIsEmptyPred : CPDLPred
    {
        public override IEnumerable<PDLPred> GetConcretizations(int distance, IEnumerable<char> alphabet)
        {
            yield return new PDLEmptyString();
        }

        public override PDLPred InterpretModel(IList<char> alphabet, Context context, Model model)
        {
            return new PDLEmptyString();
        }

        public override void ToSMTConstraints(Context z3Context, Solver z3Solver, int alphabetSize, VariableCache variableGenerator)
        {
            // Take a constraint variable just for good measure
            this.constraintVariable = variableGenerator.GetFreshVariableName();
            ArithExpr myVariable = z3Context.MkIntConst(this.constraintVariable);
            z3Solver.Assert(z3Context.MkEq(myVariable, z3Context.MkInt(0)));
        }

        public override HashSet<string> CollectChoiceVariables(HashSet<string> currentVals)
        {
            currentVals.Add(this.constraintVariable);
            return currentVals;
        }
    }

    /// <summary>
    /// Generates the following formulas:
    ///     - set1 SUBSET set2
    /// </summary>
    public class CPDLSubsetPred : CPDLPred
    {
        CPDLSet set1, set2;

        public CPDLSubsetPred(CPDLSet set1, CPDLSet set2)
        {
            this.set1 = set1;
            this.set2 = set2;
        }

        public override IEnumerable<PDLPred> GetConcretizations(int distance, IEnumerable<char> alphabet)
        {
            foreach (PDLSet set1Conc in this.set1.GetConcretizations(distance, alphabet))
            {
                foreach (PDLSet set2Conc in this.set2.GetConcretizations(distance, alphabet))
                {
                    yield return new PDLSubset(set1Conc, set2Conc);
                }
            }
        }

        public override PDLPred InterpretModel(IList<char> alphabet, Context context, Model model)
        {
            PDLSet set1Conc = this.set1.InterpretModel(alphabet, context, model);
            PDLSet set2Conc = this.set2.InterpretModel(alphabet, context, model);
            return new PDLSubset(set1Conc, set2Conc);
        }

        public override void ToSMTConstraints(Context z3Context, Solver z3Solver, int alphabetSize, VariableCache variableGenerator)
        {
            this.constraintVariable = variableGenerator.GetFreshVariableName();
            ArithExpr myVariable = z3Context.MkIntConst(this.constraintVariable);
            z3Solver.Assert(z3Context.MkEq(myVariable, z3Context.MkInt(0)));
            this.set1.ToSMTConstraints(z3Context, z3Solver, alphabetSize, variableGenerator);
            this.set2.ToSMTConstraints(z3Context, z3Solver, alphabetSize, variableGenerator);
        }

        public override HashSet<string> CollectChoiceVariables(HashSet<string> currentVals)
        {
            currentVals.Add(this.constraintVariable);
            currentVals = this.set1.CollectChoiceVariables(currentVals);
            return this.set2.CollectChoiceVariables(currentVals);
        }
    }

    public class CPDLIsStringPred : CPDLPred
    {
        CPDLString original;

        public CPDLIsStringPred(CPDLString original)
        {
            this.original = original;
        }

        public override IEnumerable<PDLPred> GetConcretizations(int distance, IEnumerable<char> alphabet)
        {
            foreach (string origConc in this.original.GetConcretizations(distance, alphabet))
            {
                yield return new PDLIsString(origConc);
            }
        }

        public override PDLPred InterpretModel(IList<char> alphabet, Context context, Model model)
        {
            string stringConc = this.original.InterpretModel(alphabet, context, model);
            return new PDLIsString(stringConc);
        }

        public override void ToSMTConstraints(Context z3Context, Solver z3Solver, int alphabetSize, VariableCache variableGenerator)
        {
            this.constraintVariable = variableGenerator.GetFreshVariableName();
            ArithExpr myVariable = z3Context.MkIntConst(this.constraintVariable);
            z3Solver.Assert(z3Context.MkEq(myVariable, z3Context.MkInt(0)));
            this.original.ToSMTConstraints(z3Context, z3Solver, alphabetSize, variableGenerator);
        }

        public override HashSet<string> CollectChoiceVariables(HashSet<string> currentVals)
        {
            currentVals.Add(this.constraintVariable);
            return this.original.CollectChoiceVariables(currentVals);
        }
    }

    public class CPDLContainsPred : CPDLPred
    {
        CPDLString original;

        public CPDLContainsPred(CPDLString original)
        {
            this.original = original;
        }

        public override IEnumerable<PDLPred> GetConcretizations(int distance, IEnumerable<char> alphabet)
        {
            foreach (string origConc in this.original.GetConcretizations(distance, alphabet))
            {
                yield return new PDLContains(origConc);
            }
        }

        public override PDLPred InterpretModel(IList<char> alphabet, Context context, Model model)
        {
            string stringConc = this.original.InterpretModel(alphabet, context, model);
            return new PDLContains(stringConc);
        }

        public override void ToSMTConstraints(Context z3Context, Solver z3Solver, int alphabetSize, VariableCache variableGenerator)
        {
            this.constraintVariable = variableGenerator.GetFreshVariableName();
            ArithExpr myVariable = z3Context.MkIntConst(this.constraintVariable);
            z3Solver.Assert(z3Context.MkEq(myVariable, z3Context.MkInt(0)));
            this.original.ToSMTConstraints(z3Context, z3Solver, alphabetSize, variableGenerator);
        }

        public override HashSet<string> CollectChoiceVariables(HashSet<string> currentVals)
        {
            currentVals.Add(this.constraintVariable);
            return this.original.CollectChoiceVariables(currentVals);
        }
    }

    /// <summary>
    /// Generates the following positions:
    ///     - x
    /// where x is the name of the first-order variable in the original formula
    /// </summary>
    public class CPDLFirstOrderVarPos : CPDLPos
    {
        private readonly string variable;

        public CPDLFirstOrderVarPos(string variable)
        {
            this.variable = variable;
        }

        public override IEnumerable<PDLPos> GetConcretizations(int distance, IEnumerable<char> alphabet)
        {
            yield return new PDLPosVar(this.variable);
        }

        public override PDLPos InterpretModel(IList<char> alphabet, Context context, Model model)
        {
            return new PDLPosVar(this.variable);
        }

        public override void ToSMTConstraints(Context z3Context, Solver z3Solver, int alphabetSize, VariableCache variableGenerator)
        {
            this.constraintVariable = variableGenerator.GetFreshVariableName();
            ArithExpr myVariable = z3Context.MkIntConst(this.constraintVariable);
            z3Solver.Assert(z3Context.MkEq(myVariable, z3Context.MkInt(0)));
        }

        public override HashSet<string> CollectChoiceVariables(HashSet<string> currentVals)
        {
            currentVals.Add(this.constraintVariable);
            return currentVals;
        }
    }

    /// <summary>
    /// Generates the following positions:
    ///     - first
    ///     - last
    /// </summary>
    public class CPDLFirstOrLastPos : CPDLPos
    {
        public override IEnumerable<PDLPos> GetConcretizations(int distance, IEnumerable<char> alphabet)
        {
            yield return new PDLFirst();
            yield return new PDLLast();
        }

        public override PDLPos InterpretModel(IList<char> alphabet, Context context, Model model)
        {
            int concChoice = this.GetConcChoice(context, model);
            if (concChoice == 0) { return new PDLFirst(); }
            else { return new PDLLast(); }
        }

        public override void ToSMTConstraints(Context z3Context, Solver z3Solver, int alphabetSize, VariableCache variableGenerator)
        {
            this.constraintVariable = variableGenerator.GetFreshVariableName();
            ArithExpr myVariable = z3Context.MkIntConst(this.constraintVariable);
            z3Solver.Assert(z3Context.MkLe(z3Context.MkInt(0), myVariable));
            z3Solver.Assert(z3Context.MkLe(myVariable, z3Context.MkInt(1)));
        }

        public override HashSet<string> CollectChoiceVariables(HashSet<string> currentVals)
        {
            currentVals.Add(this.constraintVariable);
            return currentVals;
        }
    }

    /// <summary>
    /// Generates the following positions:
    ///     - Pred^n (p)
    ///     - Succ^n (p)
    /// where n is dependent on the number of successive applications of Pred/Succ in the original formula
    /// </summary>
    public class CPDLPredOrSuccPos : CPDLPos
    {
        private readonly CPDLInteger repetitions;
        private readonly CPDLPos operand;

        public CPDLPredOrSuccPos(CPDLInteger repetitions, CPDLPos operand)
        {
            this.repetitions = repetitions;
            this.operand = operand;
        }

        public override IEnumerable<PDLPos> GetConcretizations(int distance, IEnumerable<char> alphabet)
        {
            foreach (PDLPos concPosition in this.operand.GetConcretizations(distance, alphabet))
            {
                foreach (int n in this.repetitions.GetConcretizations(distance))
                {
                    yield return this.NthPredecessor(n, concPosition);
                    yield return this.NthSuccessor(n, concPosition);
                }
            }
        }

        public override PDLPos InterpretModel(IList<char> alphabet, Context context, Model model)
        {
            int concChoice = this.GetConcChoice(context, model);
            int n = this.repetitions.InterpretModel(alphabet, context, model);
            PDLPos concPosition = this.operand.InterpretModel(alphabet, context, model);
            if (concChoice == 0) { return this.NthPredecessor(n, concPosition); }
            else { return this.NthSuccessor(n, concPosition); }
        }

        private PDLPos NthPredecessor(int n, PDLPos operand)
        {
            PDLPos returnValue = operand;
            for (int i = 0; i < n; ++i)
            {
                returnValue = new PDLPredecessor(returnValue);
            }
            return returnValue;
        }

        private PDLPos NthSuccessor(int n, PDLPos operand)
        {
            PDLPos returnValue = operand;
            for (int i = 0; i < n; ++i)
            {
                returnValue = new PDLSuccessor(returnValue);
            }
            return returnValue;
        }

        public override void ToSMTConstraints(Context z3Context, Solver z3Solver, int alphabetSize, VariableCache variableGenerator)
        {
            this.constraintVariable = variableGenerator.GetFreshVariableName();
            ArithExpr myVariable = z3Context.MkIntConst(this.constraintVariable);
            z3Solver.Assert(z3Context.MkLe(z3Context.MkInt(0), myVariable));
            z3Solver.Assert(z3Context.MkLe(myVariable, z3Context.MkInt(1)));
            this.operand.ToSMTConstraints(z3Context, z3Solver, alphabetSize, variableGenerator);
            this.repetitions.ToSMTConstraints(z3Context, z3Solver, alphabetSize, variableGenerator);
        }

        public override HashSet<string> CollectChoiceVariables(HashSet<string> currentVals)
        {
            currentVals.Add(this.constraintVariable);
            currentVals = this.operand.CollectChoiceVariables(currentVals);
            return this.repetitions.CollectChoiceVariables(currentVals);
        }
    }

    /// <summary>
    /// Generates the following positions:
    ///     - frstOc(s)
    ///     - lastOc(s)
    /// </summary>
    public class CPDLFirstOrLastOccPos : CPDLPos
    {
        private readonly CPDLString original;

        public CPDLFirstOrLastOccPos(CPDLString original)
        {
            this.original = original;
        }

        public override IEnumerable<PDLPos> GetConcretizations(int distance, IEnumerable<char> alphabet)
        {
            foreach( string concString in this.original.GetConcretizations(distance, alphabet)) {
                yield return new PDLFirstOcc(concString);
                yield return new PDLLastOcc(concString);
            }
        }

        public override PDLPos InterpretModel(IList<char> alphabet, Context context, Model model)
        {
            int concChoice = this.GetConcChoice(context, model);
            string concString = this.original.InterpretModel(alphabet, context, model);
            if (concChoice == 0) { return new PDLFirstOcc(concString); }
            else { return new PDLLastOcc(concString); }
        }

        public override void ToSMTConstraints(Context z3Context, Solver z3Solver, int alphabetSize, VariableCache variableGenerator)
        {
            this.constraintVariable = variableGenerator.GetFreshVariableName();
            ArithExpr myVariable = z3Context.MkIntConst(this.constraintVariable);
            z3Solver.Assert(z3Context.MkLe(z3Context.MkInt(0), myVariable));
            z3Solver.Assert(z3Context.MkLe(myVariable, z3Context.MkInt(1)));
            this.original.ToSMTConstraints(z3Context, z3Solver, alphabetSize, variableGenerator);
        }

        public override HashSet<string> CollectChoiceVariables(HashSet<string> currentVals)
        {
            currentVals.Add(this.constraintVariable);
            return this.original.CollectChoiceVariables(currentVals);
        }
    }

    /// <summary>
    /// Generates the following sets:
    ///     - X
    /// where X is the name of a set-variable in the original formula
    /// </summary>
    public class CPDLVariableSet : CPDLSet
    {
        private readonly string variableName;

        public CPDLVariableSet(string variableName)
        {
            this.variableName = variableName;
        }

        public override IEnumerable<PDLSet> GetConcretizations(int distance, IEnumerable<char> alphabet)
        {
            yield return new PDLSetVar(this.variableName);
        }

        public override PDLSet InterpretModel(IList<char> alphabet, Context context, Model model)
        {
            return new PDLSetVar(this.variableName);
        }

        public override void ToSMTConstraints(Context z3Context, Solver z3Solver, int alphabetSize, VariableCache variableGenerator)
        {
            this.constraintVariable = variableGenerator.GetFreshVariableName();
            ArithExpr myVariable = z3Context.MkIntConst(this.constraintVariable);
            z3Solver.Assert(z3Context.MkEq(myVariable, z3Context.MkInt(0)));
        }

        public override HashSet<string> CollectChoiceVariables(HashSet<string> currentVals)
        {
            currentVals.Add(this.constraintVariable);
            return currentVals;
        }
    }

    /// <summary>
    /// Generates the following sets:
    ///     - indOf(s)
    /// </summary>
    public class CPDLIndOfSet : CPDLSet
    {
        private readonly CPDLString original;

        public CPDLIndOfSet(CPDLString original)
        {
            this.original = original;
        }

        public override IEnumerable<PDLSet> GetConcretizations(int distance, IEnumerable<char> alphabet)
        {
            foreach (string concString in this.original.GetConcretizations(distance, alphabet))
            {
                yield return new PDLIndicesOf(concString);
            }
        }

        public override PDLSet InterpretModel(IList<char> alphabet, Context context, Model model)
        {
            string concString = this.original.InterpretModel(alphabet, context, model);
            return new PDLIndicesOf(concString);
        }

        public override void ToSMTConstraints(Context z3Context, Solver z3Solver, int alphabetSize, VariableCache variableGenerator)
        {
            this.constraintVariable = variableGenerator.GetFreshVariableName();
            ArithExpr myVariable = z3Context.MkIntConst(this.constraintVariable);
            z3Solver.Assert(z3Context.MkEq(myVariable, z3Context.MkInt(0)));
            this.original.ToSMTConstraints(z3Context, z3Solver, alphabetSize, variableGenerator);
        }

        public override HashSet<string> CollectChoiceVariables(HashSet<string> currentVals)
        {
            currentVals.Add(this.constraintVariable);
            return this.original.CollectChoiceVariables(currentVals);
        }
    }

    /// <summary>
    /// Generates the following sets:
    ///     - s1 UNION s2
    ///     - s1 INTER s2
    /// </summary>
    public class CPDLSetOperatorSet : CPDLSet
    {
        private readonly CPDLSet lhs, rhs;

        public CPDLSetOperatorSet(CPDLSet lhs, CPDLSet rhs)
        {
            this.lhs = lhs;
            this.rhs = rhs;
        }

        public override IEnumerable<PDLSet> GetConcretizations(int distance, IEnumerable<char> alphabet)
        {
            foreach (PDLSet lhsConc in this.lhs.GetConcretizations(distance, alphabet))
            {
                foreach (PDLSet rhsConc in this.rhs.GetConcretizations(distance, alphabet))
                {
                    yield return new PDLUnion(lhsConc, rhsConc);
                    yield return new PDLIntersect(lhsConc, rhsConc);
                }
            }
        }

        public override PDLSet InterpretModel(IList<char> alphabet, Context context, Model model)
        {
            int concChoice = this.GetConcChoice(context, model);
            PDLSet lhsConc = this.lhs.InterpretModel(alphabet, context, model);
            PDLSet rhsConc = this.rhs.InterpretModel(alphabet, context, model);
            if (concChoice == 0) { return new PDLUnion(lhsConc, rhsConc); }
            else { return new PDLIntersect(lhsConc, rhsConc); }
        }

        public override void ToSMTConstraints(Context z3Context, Solver z3Solver, int alphabetSize, VariableCache variableGenerator)
        {
            this.constraintVariable = variableGenerator.GetFreshVariableName();
            ArithExpr myVariable = z3Context.MkIntConst(this.constraintVariable);
            z3Solver.Assert(z3Context.MkLe(z3Context.MkInt(0), myVariable));
            z3Solver.Assert(z3Context.MkLe(myVariable, z3Context.MkInt(1)));
            this.lhs.ToSMTConstraints(z3Context, z3Solver, alphabetSize, variableGenerator);
            this.rhs.ToSMTConstraints(z3Context, z3Solver, alphabetSize, variableGenerator);
        }

        public override HashSet<string> CollectChoiceVariables(HashSet<string> currentVals)
        {
            currentVals.Add(this.constraintVariable);
            currentVals = this.lhs.CollectChoiceVariables(currentVals);
            return this.rhs.CollectChoiceVariables(currentVals);
        }
    }

    /// <summary>
    /// Generates the following sets:
    ///     - ALL
    /// </summary>
    public class CPDLAllSet : CPDLSet
    {
        public override IEnumerable<PDLSet> GetConcretizations(int distance, IEnumerable<char> alphabet)
        {
            yield return new PDLAllPos();
        }

        public override PDLSet InterpretModel(IList<char> alphabet, Context context, Model model)
        {
            return new PDLAllPos();
        }

        public override void ToSMTConstraints(Context z3Context, Solver z3Solver, int alphabetSize, VariableCache variableGenerator)
        {
            this.constraintVariable = variableGenerator.GetFreshVariableName();
            ArithExpr myVariable = z3Context.MkIntConst(this.constraintVariable);
            z3Solver.Assert(z3Context.MkEq(myVariable, z3Context.MkInt(0)));
        }

        public override HashSet<string> CollectChoiceVariables(HashSet<string> currentVals)
        {
            currentVals.Add(this.constraintVariable);
            return currentVals;
        }
    }

    /// <summary>
    /// Generates the following sets:
    ///     - COMP p1
    /// for each COMP in LT, LEQ, GT, GEQ
    /// </summary>
    public class CPDLPosComparisonSet : CPDLSet
    {
        private readonly CPDLPos original;

        public CPDLPosComparisonSet(CPDLPos original)
        {
            this.original = original;
        }

        public override IEnumerable<PDLSet> GetConcretizations(int distance, IEnumerable<char> alphabet)
        {
            foreach (PDLPos conc in this.original.GetConcretizations(distance, alphabet))
            {
                foreach (PDLComparisonOperator op in Enum.GetValues(typeof(PDLComparisonOperator)))
                {
                    yield return new PDLSetCmpPos(conc, op);
                }
            }
        }

        public override PDLSet InterpretModel(IList<char> alphabet, Context context, Model model)
        {
            int concChoice = this.GetConcChoice(context, model);
            PDLPos conc = this.original.InterpretModel(alphabet, context, model);
            switch (concChoice)
            {
                case 0: return new PDLSetCmpPos(conc, PDLComparisonOperator.Eq);
                case 1: return new PDLSetCmpPos(conc, PDLComparisonOperator.Ge);
                case 2: return new PDLSetCmpPos(conc, PDLComparisonOperator.Geq);
                case 3: return new PDLSetCmpPos(conc, PDLComparisonOperator.Le);
                case 4: return new PDLSetCmpPos(conc, PDLComparisonOperator.Leq);
            }
            return null;
        }

        public override void ToSMTConstraints(Context z3Context, Solver z3Solver, int alphabetSize, VariableCache variableGenerator)
        {
            this.constraintVariable = variableGenerator.GetFreshVariableName();
            ArithExpr myVariable = z3Context.MkIntConst(this.constraintVariable);
            z3Solver.Assert(z3Context.MkLe(z3Context.MkInt(0), myVariable));
            z3Solver.Assert(z3Context.MkLe(myVariable, z3Context.MkInt(4)));
            this.original.ToSMTConstraints(z3Context, z3Solver, alphabetSize, variableGenerator);
        }

        public override HashSet<string> CollectChoiceVariables(HashSet<string> currentVals)
        {
            currentVals.Add(this.constraintVariable);
            return this.original.CollectChoiceVariables(currentVals);
        }
    }

    public class CPDLPredSet : CPDLSet
    {
        private string FOVar;
        private CPDLPred pred;

        public CPDLPredSet(string FOVar, CPDLPred pred) {
            this.FOVar = FOVar;
            this.pred = pred;
        }


        public override IEnumerable<PDLSet> GetConcretizations(int distance, IEnumerable<char> alphabet)
        {
            foreach (PDLPred predConc in this.pred.GetConcretizations(distance, alphabet))
            {
                yield return new PDLPredSet(this.FOVar, predConc);
            }
        }

        public override PDLSet InterpretModel(IList<char> alphabet, Context context, Model model)
        {
            PDLPred predConc = this.pred.InterpretModel(alphabet, context, model);
            return new PDLPredSet(this.FOVar, predConc);
        }

        public override void ToSMTConstraints(Context z3Context, Solver z3Solver, int alphabetSize, VariableCache variableGenerator)
        {
            this.constraintVariable = variableGenerator.GetFreshVariableName();
            ArithExpr myVariable = z3Context.MkIntConst(this.constraintVariable);
            z3Solver.Assert(z3Context.MkEq(myVariable, z3Context.MkInt(0)));
            this.pred.ToSMTConstraints(z3Context, z3Solver, alphabetSize, variableGenerator);
        }

        public override HashSet<string> CollectChoiceVariables(HashSet<string> currentVals)
        {
            currentVals.Add(this.constraintVariable);
            return this.pred.CollectChoiceVariables(currentVals);
        }
    }

    public class CPDLString
    {
        private readonly string originalValue;
        private string constraintVariable;

        public CPDLString(string originalValue)
        {
            this.originalValue = originalValue;
        }

        public IEnumerable<string> GetConcretizations(int distance, IEnumerable<char> alphabet)
        {
            HashSet<string> nonDoubledConcretizations = new HashSet<string>();
            // Since GetConcretizations may double results, make sure that we delete these
            foreach (string concretization in GetConcretizations(distance, alphabet, this.originalValue))
            {
                if (!nonDoubledConcretizations.Contains(concretization)) { nonDoubledConcretizations.Add(concretization); }
            }

            return nonDoubledConcretizations;
        }

        public string InterpretModel(IList<char> alphabet, Context context, Model model)
        {
            int concChoice = ((IntNum)model.ConstInterp(context.MkIntConst(this.constraintVariable))).Int;
            int alphabetSize = alphabet.Count;
            string returnValue = "";
            for (int currentIndex = 0; currentIndex < this.originalValue.Length; ++currentIndex)
            {
                int currentChoice = concChoice % alphabetSize;
                returnValue += alphabet[currentChoice];
                concChoice -= currentChoice;
                concChoice = concChoice / alphabetSize;
            }
            return returnValue;
        }

        private static IEnumerable<string> GetConcretizations(int distance, IEnumerable<char> alphabet, string toConcretize)
        {
            if(distance == 0) {
                yield return toConcretize;
                yield break;
            }

            if (toConcretize.Equals(""))
            {
                // Since we do not have any characters to mutate anymore, return nothing
                yield break;
            }

            // Possibility 1: Do nothing and do all changes in the rest of the string
            foreach(string remainingConcretization in GetConcretizations(distance, alphabet, toConcretize.Substring(1))) {
                yield return toConcretize.Substring(0, 1) + remainingConcretization;
            }

            // Possibility 2: Mutate the first character
            foreach(string remainingConcretization in GetConcretizations(distance - 1, alphabet, toConcretize.Substring(1))) {
                foreach(char character in alphabet) {
                    if(!character.Equals(toConcretize.ElementAt(0))) {
                        yield return character + remainingConcretization;
                    }
                }
            }

        }

        public void ToSMTConstraints(Context z3Context, Solver z3Solver, int alphabetSize, VariableCache variableGenerator)
        {
            this.constraintVariable = variableGenerator.GetStringChoiceVariable(this.originalValue);
            ArithExpr myVariable = z3Context.MkIntConst(this.constraintVariable);
            z3Solver.Assert(z3Context.MkLe(z3Context.MkInt(0), myVariable));
            /* We have |s| * |\Sigma| options for concretizing this string
             * Thus, since we are 0-based: originalValue.Length * alphabet - 1 */
            z3Solver.Assert(z3Context.MkLe(myVariable, z3Context.MkInt(this.originalValue.Length * alphabetSize - 1)));
        }

        public HashSet<string> CollectChoiceVariables(HashSet<string> currentVals)
        {
            currentVals.Add(this.constraintVariable);
            return currentVals;
        }
    }

    public class CPDLInteger
    {
        private readonly int originalValue;
        private readonly bool includeZero;
        private string constraintVariable;

        public CPDLInteger(int originalValue) : this(originalValue, true) { }

        public CPDLInteger(int originalValue, bool includeZero)
        {
            this.originalValue = originalValue;
            this.includeZero = includeZero;
        }

        public IEnumerable<int> GetConcretizations(int distance) {
            int startValue = Math.Max(originalValue - distance, 0);
            for (int i = startValue; i <= originalValue + distance; ++i)
            {
                yield return i;
            }
        }

        public int InterpretModel(IList<char> alphabet, Context context, Model model)
        {
            int concChoice = ((IntNum)model.ConstInterp(context.MkIntConst(this.constraintVariable))).Int;
            Debug.Assert(concChoice != 0 || this.includeZero == true);
            return concChoice;
        }

        public void ToSMTConstraints(Context z3Context, Solver z3Solver, int alphabetSize, VariableCache variableGenerator)
        {
            this.constraintVariable = variableGenerator.GetIntegerChoiceVariable(this.originalValue);
            ArithExpr myVariable = z3Context.MkIntConst(this.constraintVariable);
            // For now, just concretize in the range [floor(origVal / 2), ceil(origVal * 1.5)]
            z3Solver.Assert(z3Context.MkLe(z3Context.MkInt((int)(this.originalValue * 0.5)), myVariable));
            z3Solver.Assert(z3Context.MkLe(myVariable, z3Context.MkInt((int)(this.originalValue * 1.5))));

            if (this.includeZero == false)
            {
                z3Solver.Assert(z3Context.MkNot(z3Context.MkEq(z3Context.MkInt(0), myVariable)));
            }
        }

        public HashSet<string> CollectChoiceVariables(HashSet<string> currentVals)
        {
            currentVals.Add(this.constraintVariable);
            return currentVals;
        }
    }

    public class CPDLChar
    {
        private readonly char originalValue;
        private string constraintVariable;

        public CPDLChar(char originalValue){
            this.originalValue = originalValue;
        }

        public char InterpretModel(IList<char> alphabet, Context context, Model model)
        {
            int concChoice = ((IntNum)model.ConstInterp(context.MkIntConst(this.constraintVariable))).Int;
            return alphabet[concChoice];
        }

        public void ToSMTConstraints(Context z3Context, Solver z3Solver, int alphabetSize, VariableCache variableGenerator)
        {
            this.constraintVariable = variableGenerator.GetCharChoiceVariable(this.originalValue);
            ArithExpr myVariable = z3Context.MkIntConst(this.constraintVariable);
            z3Solver.Assert(z3Context.MkLe(z3Context.MkInt(0), myVariable));
            z3Solver.Assert(z3Context.MkLe(myVariable, z3Context.MkInt(alphabetSize - 1)));
        }

        public HashSet<string> CollectChoiceVariables(HashSet<string> currentVals)
        {
            currentVals.Add(this.constraintVariable);
            return currentVals;
        }
    }


    public class ProblemGeneration
    {

        public static IEnumerable<PDLPred> GeneratePDLWithEDn(PDLPred phi, IEnumerable<char> alphabet, VariableCache.ConstraintMode constConst, PdlFilter.Filtermode filtermode)
        {
            CPDLPred choiceTree = phi.GetCPDL();
            HashSet<char> alphabetHashSet = new HashSet<char>(alphabet);

            PdlFilter filter = PdlFilter.Create(filtermode, phi, alphabetHashSet);

            // Concretize yields all feasible concretizations of the choiceTree
            foreach(PDLPred candidate in Concretize(choiceTree, alphabetHashSet, constConst)) {
                if (filter.KeepPredicate(candidate) == true)
                {
                    yield return candidate;
                }
            }
        }

        private static IEnumerable<PDLPred> Concretize(CPDLPred choicePred, HashSet<char> alphabet, VariableCache.ConstraintMode constraintMode) {
            Context z3Context = new Context();
            Solver z3Solver = z3Context.MkSolver();

            GenerateConstraints(choicePred, alphabet, constraintMode, z3Context, z3Solver);

            List<char> alphabetList = new List<char>(alphabet);
            IEnumerable<string> choiceVariables = choicePred.GetChoiceVariables();
            while (z3Solver.Check() == Status.SATISFIABLE)
            {
                yield return choicePred.InterpretModel(alphabetList, z3Context, z3Solver.Model);
                ExcludeLastModel(choiceVariables, z3Context, z3Solver);
            }
        }

        private static void GenerateConstraints(CPDLPred choicePred, HashSet<char> alphabet, VariableCache.ConstraintMode constraintMode, Context z3Context, Solver z3Solver)
        {
            VariableCache variableGenerator = VariableCache.Create(constraintMode);
            choicePred.ToSMTConstraints(z3Context, z3Solver, alphabet.Count, variableGenerator);
            variableGenerator.GenerateAdditionalConstraints(z3Context, z3Solver);
        }

        private static void ExcludeLastModel(IEnumerable<string> choiceVariables, Context z3Context, Solver z3Solver)
        {
            Model lastModel = z3Solver.Model;
            BoolExpr characteristicFormula = CreateCharacteristicFormula(choiceVariables, z3Context, lastModel);
            z3Solver.Assert(z3Context.MkNot(characteristicFormula));
        }

        private static BoolExpr CreateCharacteristicFormula(IEnumerable<string> choiceVariables, Context z3Context, Model lastModel)
        {
            BoolExpr characteristicFormula = null;
            foreach (string choiceVariable in choiceVariables)
            {
                BoolExpr currentAssignment = CreateAssignmentFormula(z3Context, lastModel, choiceVariable);
                if (characteristicFormula == null)
                {
                    characteristicFormula = currentAssignment;
                }
                else
                {
                    characteristicFormula = z3Context.MkAnd(characteristicFormula, currentAssignment);
                }
            }
            return characteristicFormula;
        }

        private static BoolExpr CreateAssignmentFormula(Context z3Context, Model lastModel, string choiceVariable)
        {
            ArithExpr z3Variable = z3Context.MkIntConst(choiceVariable);
            ArithExpr assignment = (ArithExpr)lastModel.ConstInterp(z3Variable);
            BoolExpr currentAssignment = z3Context.MkEq(z3Variable, assignment);
            return currentAssignment;
        }

        private static Boolean KeepProblem(PDLPred original, PDLPred phi, int distance, HashSet<char> alphabet, CharSetSolver solver)
        {
            return true;
        }

        public static int GetSMTVariables(PDLPred phi, IEnumerable<char> alphabet)
        {
            CPDLPred choiceTree = phi.GetCPDL();
            HashSet<char> alphabetHashSet = new HashSet<char>(alphabet);

            Context z3Context = new Context();
            Solver z3Solver = z3Context.MkSolver();

            VariableCache variableGenerator = VariableCache.Create(VariableCache.ConstraintMode.BOTH);
            choiceTree.ToSMTConstraints(z3Context, z3Solver, alphabetHashSet.Count, variableGenerator);
            variableGenerator.GenerateAdditionalConstraints(z3Context, z3Solver);

            return variableGenerator.GetNumVariables();
        }
    }

    public abstract class VariableCache
    {
        public enum ConstraintMode
        {
            NONE, // Generated constants follow no relation among each other
            EQUAL, // If two constants were equal in the original formula, they are equal in the generated formula
            INEQUAL, // If two constants were inequal in the original formula, they are inequal in the generated formula
            BOTH // Two constants are equal in the generated formula iff they were equal in the original formula
        }

        protected int nextVariableNumber = 0;
        protected IDictionary<int, string> integerChoiceVariables = new Dictionary<int, string>();
        protected IDictionary<string, string> stringChoiceVariables = new Dictionary<string, string>();
        protected IDictionary<char, string> charChoiceVariables = new Dictionary<char, string>();

        private VariableConstraintGenerator constraintGenerator;

        public static VariableCache Create(ConstraintMode constraintMode)
        {
            switch (constraintMode)
            {
                case ConstraintMode.NONE: return new UnconstrainedVariableCache(new NoConstraintGenerator());
                case ConstraintMode.EQUAL: return new PreserveEqualityCache(new NoConstraintGenerator());
                case ConstraintMode.INEQUAL:
                    {
                        PreserveInequalityGenerator constraintGenerator = new PreserveInequalityGenerator();
                        VariableCache variableCache = new UnconstrainedVariableCache(constraintGenerator);
                        constraintGenerator.variableCache = variableCache;
                        return variableCache;
                    }
                case ConstraintMode.BOTH:
                    {
                        PreserveInequalityGenerator constraintGenerator = new PreserveInequalityGenerator();
                        VariableCache variableCache = new PreserveEqualityCache(constraintGenerator);
                        constraintGenerator.variableCache = variableCache;
                        return variableCache;
                    }
            }
            return null;
        }

        protected VariableCache(VariableConstraintGenerator constraintGenerator)
        {
            this.constraintGenerator = constraintGenerator;
        }

        public ICollection<string> GetIntegerVariables()
        {
            return new List<string>(this.integerChoiceVariables.Values);
        }

        public ICollection<string> GetStringVariables()
        {
            return new List<string>(this.stringChoiceVariables.Values);
        }

        public ICollection<string> GetCharVariables()
        {
            return new List<string>(this.charChoiceVariables.Values);
        }

        abstract public string GetFreshVariableName();
        abstract public string GetIntegerChoiceVariable(int originalValue);
        abstract public string GetStringChoiceVariable(string originalValue);
        abstract public string GetCharChoiceVariable(char originalValue);

        public void GenerateAdditionalConstraints(Context z3Context, Solver z3Solver)
        {
            this.constraintGenerator.GenerateVariableConstraints(z3Context, z3Solver);
        }

        internal int GetNumVariables()
        {
            return this.nextVariableNumber;
        }
    }

    public class UnconstrainedVariableCache : VariableCache
    {
        public UnconstrainedVariableCache(VariableConstraintGenerator constraintGenerator)
            : base(constraintGenerator) { }

        private string getFreshVariableName(string prefix)
        {
            string returnValue = prefix + this.nextVariableNumber.ToString();
            this.nextVariableNumber += 1;
            return returnValue;
        }

        public override string GetFreshVariableName()
        {
            return this.getFreshVariableName("choiceVar_");
        }

        public override string GetCharChoiceVariable(char originalValue)
        {
            return this.getFreshVariableName("char_");
        }

        public override string GetIntegerChoiceVariable(int originalValue)
        {
            return this.getFreshVariableName("int_");
        }

        public override string GetStringChoiceVariable(string originalValue)
        {
            return this.getFreshVariableName("string_");
        }

    }

    public class PreserveEqualityCache : VariableCache
    {
        public PreserveEqualityCache(VariableConstraintGenerator constraintGenerator)
            : base(constraintGenerator) { }

        public override string GetFreshVariableName()
        {
            string returnValue = "choiceVar_" + this.nextVariableNumber.ToString();
            this.nextVariableNumber += 1;
            return returnValue;
        }

        public override string GetIntegerChoiceVariable(int originalInteger)
        {
            if (!this.integerChoiceVariables.ContainsKey(originalInteger))
            {
                this.integerChoiceVariables[originalInteger] = "int_" + this.nextVariableNumber;
                this.nextVariableNumber += 1;
            }
            return this.integerChoiceVariables[originalInteger];
            
        }

        public override string GetStringChoiceVariable(string originalString)
        {
            if (!this.stringChoiceVariables.ContainsKey(originalString))
            {
                this.stringChoiceVariables[originalString] = "string_" + this.nextVariableNumber;
                this.nextVariableNumber += 1;
            }
            return this.stringChoiceVariables[originalString];
        }

        public override string GetCharChoiceVariable(char originalChar)
        {
            if (!this.charChoiceVariables.ContainsKey(originalChar))
            {
                this.charChoiceVariables[originalChar] = "char_" + this.nextVariableNumber;
                this.nextVariableNumber += 1;
            }
            return this.charChoiceVariables[originalChar];
        }
    }

    public abstract class VariableConstraintGenerator
    {
        public abstract void GenerateVariableConstraints(Context z3Context, Solver z3Solver);
    }

    public class NoConstraintGenerator : VariableConstraintGenerator
    {
        public override void GenerateVariableConstraints(Context z3Context, Solver z3Solver)
        {
            // Since we do not need any constraints, just do nothing. Basically a null-object
            return;
        }
    }

    public class PreserveInequalityGenerator : VariableConstraintGenerator
    {
        // Must be set to a valid VariableCache by the constructing method
        public VariableCache variableCache;

        public override void GenerateVariableConstraints(Context z3Context, Solver z3Solver)
        {
            this.GenerateInequalityConstraints(this.variableCache.GetIntegerVariables(), z3Context, z3Solver);
            this.GenerateInequalityConstraints(this.variableCache.GetStringVariables(), z3Context, z3Solver);
            this.GenerateInequalityConstraints(this.variableCache.GetCharVariables(), z3Context, z3Solver);
        }

        private void GenerateInequalityConstraints(ICollection<string> vars, Context z3Context, Solver z3Solver)
        {
            List<string> varList = new List<string>(vars);
            for (int i = 0; i < varList.Count; ++i)
            {
                for (int j = i + 1; j < varList.Count; ++j)
                {
                    // Assert vars[i] != vars[j]
                    z3Solver.Assert(
                        z3Context.MkNot(
                            z3Context.MkEq(
                                z3Context.MkIntConst(varList[i]), 
                                z3Context.MkIntConst(varList[j])
                            )
                        )
                    );
                }
            }
        }
    }

    public abstract class PdlFilter
    {
        public enum Filtermode
        {
            NONE, // Generated formulae are not filtered at all
            TRIVIAL, // Presumably trivial formulae are filtered out
            STATEBASED, // Formulae that result in automata with too different statecounts are filtered out
            BOTH // We apply first trivial, then statebased
        }

        public static PdlFilter Create(Filtermode filtermode, PDLPred original, HashSet<char> alphabet) {
            switch (filtermode)
            {
                case Filtermode.NONE: return new NoFilter();
                case Filtermode.TRIVIAL: return new TrivialFormulaFilter(alphabet, 4);
                case Filtermode.STATEBASED: return new DfaStateNumberFilter(original, alphabet);
                case Filtermode.BOTH:
                    PdlFilter trivialFilter = new TrivialFormulaFilter(alphabet, 4);
                    PdlFilter stateFilter = new DfaStateNumberFilter(original, alphabet);
                    return new ConsFilter(trivialFilter, stateFilter);
            }
            return null;
        }

        public abstract bool KeepPredicate(PDLPred candidate);
    }

    public class ConsFilter : PdlFilter
    {
        private readonly PdlFilter firstFilter, secondFilter;

        public ConsFilter(PdlFilter firstFilter, PdlFilter secondFilter)
        {
            this.firstFilter = firstFilter;
            this.secondFilter = secondFilter;
        }

        public override bool KeepPredicate(PDLPred candidate)
        {
            return this.firstFilter.KeepPredicate(candidate) && this.secondFilter.KeepPredicate(candidate);
        }
    }

    public class NoFilter : PdlFilter
    {
        public override bool KeepPredicate(PDLPred candidate) { return true; }
    }

    /// <summary>
    /// Estimates if the formula is too simple and filters out those that are
    /// </summary>
    public class TrivialFormulaFilter : PdlFilter
    {
        private readonly IList<string> testStrings;

        public TrivialFormulaFilter(IEnumerable<char> alphabet, int maxLength)
        {
            this.testStrings = this.GenerateStringsUpToLength(alphabet, maxLength);
        }

        private IList<string> GenerateStringsUpToLength(IEnumerable<char> alphabet, int maxLength)
        {
            IList<string> returnValue = new List<string>();
            IList<string> lastIteration = new List<string>();
            foreach (char character in alphabet)
            {
                lastIteration.Add(character.ToString());
                returnValue.Add(character.ToString());
            }

            for (int currentLength = 0; currentLength < maxLength; ++currentLength)
            {
                IList<string> currentIteration = new List<string>();
                foreach (string lastIterationResult in lastIteration)
                {
                    foreach (char character in alphabet)
                    {
                        String currentResult = lastIterationResult + character;
                        currentIteration.Add(currentResult);
                        returnValue.Add(currentResult);
                    }
                }
                lastIteration = currentIteration;
            }

            return returnValue;
        }

        /// <summary>
        /// Discards a formula if it has the same result on all test strings. The idea is that this formula is probably too
        /// easy.
        /// </summary>
        /// <param name="candidates"></param>
        /// <returns></returns>
        public override bool KeepPredicate(PDLPred candidate)
        {
            bool firstResult = candidate.Eval(this.testStrings[0], new Dictionary<string, int>());
            for (int testStringIndex = 1; testStringIndex < this.testStrings.Count; ++testStringIndex)
            {
                string testString = this.testStrings[testStringIndex];
                bool currentResult = candidate.Eval(testString, new Dictionary<string, int>());
                if (currentResult != firstResult) { 
                    // Since a single differing result is enough, we can return from here
                    return true;
                }
            }
            return false;
        }
    }

    public class DfaStateNumberFilter : PdlFilter
    {
        private readonly int numOriginalStates;
        private readonly HashSet<char> alphabet;
        private readonly CharSetSolver charsetSolver;

        public DfaStateNumberFilter(PDLPred original, HashSet<char> alphabet)
        {
            this.charsetSolver = new CharSetSolver();
            this.alphabet = alphabet;
            this.numOriginalStates = original.GetDFA(alphabet, this.charsetSolver).StateCount;
        }

        public override bool KeepPredicate(PDLPred candidate)
        {
            Automaton<BDD> candidateDfa = candidate.GetDFA(this.alphabet, this.charsetSolver);

            int stateDifference = Math.Abs(numOriginalStates - candidateDfa.StateCount);
            return IsAcceptableStateDifference(stateDifference);
        }

        private bool IsAcceptableStateDifference(int stateDifference)
        {
            // Allow max. 20% more or less states
            return (((double)stateDifference) / ((double)this.numOriginalStates) < 0.2);
        }
    }
}
