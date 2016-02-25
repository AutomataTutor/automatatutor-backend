using AutomataPDL;
using Microsoft.Automata;
using System.Collections.Generic;
using System;
namespace Measurement
{
    public class MeasurementResultSet
    {
        public readonly PDLPred originalFormula;
        public readonly Automaton<BDD> originalAutomaton;
        public readonly HashSet<char> alphabet;

        public readonly List<SingleMeasurementResult> results;

        public readonly VariableCache.ConstraintMode constraintmode;
        public readonly PdlFilter.Filtermode filtermode;
        public readonly long elapsedTime; // in milliseconds

        //public readonly double densityDiffAverage, densityDifferenceDeviation;
        //public readonly double editDistanceAverage, editDistanceDeviation;

        public MeasurementResultSet(PDLPred originalFormula, IEnumerable<PDLPred> generatedFormulas, long time, VariableCache.ConstraintMode constraintmode, 
            PdlFilter.Filtermode filtermode, HashSet<char> alphabet, IDictionary<PDLPred, SingleMeasurementResult> cache,
            IDictionary<Automaton<BDD>, SingleMeasurementResult> automatonCache)
        {
            this.originalFormula = originalFormula;
            this.alphabet = alphabet;
            this.originalAutomaton = originalFormula.GetDFA(alphabet, new CharSetSolver());

            this.constraintmode = constraintmode;
            this.filtermode = filtermode;

            this.elapsedTime = time;

            this.results = new List<SingleMeasurementResult>();

            foreach (PDLPred generatedFormula in generatedFormulas)
            {
                SingleMeasurementResult result;
                if (cache.ContainsKey(generatedFormula))
                {
                    result = cache[generatedFormula];
                }
                else
                {
                    result = SingleMeasurementResult.Create(this.originalAutomaton, generatedFormula, this.alphabet, automatonCache);
                    cache[generatedFormula] = result;
                }
                this.results.Add(result);
            }

            // Compute statistics
            /*
            double densityDiffSum = 0;
            int editDistanceSum = 0;

            foreach (SingleMeasurementResult result in this.results)
            {
                densityDiffSum += result.densityDiff;
                editDistanceSum += result.editDistance;
            }

            this.densityDiffAverage = ((double)densityDiffSum) / ((double)this.results.Count);
            this.editDistanceAverage = ((double)editDistanceSum) / ((double)this.results.Count);

            double densityDiffDeviation = 0;
            double editDistanceDeviation = 0;
            foreach (SingleMeasurementResult result in this.results)
            {
                densityDiffDeviation += Math.Pow(result.densityDiff - this.densityDiffAverage, 2.0);
                editDistanceDeviation += Math.Pow(((double)result.editDistance) - this.editDistanceAverage, 2.0);
            }
            densityDiffDeviation /= this.results.Count;
            densityDiffDeviation = Math.Sqrt(densityDiffDeviation);

            editDistanceDeviation /= this.results.Count;
            editDistanceDeviation = Math.Sqrt(editDistanceDeviation);
            */
        }

        public void Output(System.IO.StreamWriter writer)
        {
            this.OutputHeaderLine(writer);
            foreach (SingleMeasurementResult result in this.results)
            {
                this.OutputResultLine(writer, result);
            }
        }

        private void OutputHeaderLine(System.IO.StreamWriter writer)
        {
            // Format: Formula;#OrigStates;ConstraintMode;FilterMode;#Generated;Time(ms)
            string headerLine = String.Format("{0};{1};{2};{3};{4};{5}", this.originalFormula, this.originalAutomaton.StateCount, this.constraintmode, this.filtermode, this.results.Count, this.elapsedTime);
            writer.WriteLine(headerLine);
        }

        private void OutputResultLine(System.IO.StreamWriter writer, SingleMeasurementResult result)
        {
            // Format: Formula;editDistance;densityDiff
            string resultLine = String.Format("{0};{1};{2}", result.generatedFormula, result.editDistance, result.densityDiff);
            writer.WriteLine(resultLine);
        }
    }

    public class SingleMeasurementResult
    {
        public readonly PDLPred generatedFormula;
        public readonly int editDistance;
        public readonly double densityDiff;

        public static SingleMeasurementResult Create(Automaton<BDD> originalAutomaton, PDLPred generatedFormula, HashSet<char> alphabet, IDictionary<Automaton<BDD>, SingleMeasurementResult> cache)
        {
            CharSetSolver solver = new CharSetSolver();
            Automaton<BDD> generatedAutomaton = generatedFormula.GetDFA(alphabet, solver);
            SingleMeasurementResult returnValue;
            if (cache.ContainsKey(generatedAutomaton))
            {
                returnValue = cache[generatedAutomaton];
                System.Diagnostics.Debug.WriteLine("Automaton Cache Hit");
            }
            else
            {
                returnValue = new SingleMeasurementResult(originalAutomaton, generatedFormula, generatedAutomaton, alphabet);
                cache[generatedAutomaton] = returnValue;
                System.Diagnostics.Debug.WriteLine("Automaton Cache Miss");
            }
            return returnValue;
        }

        private SingleMeasurementResult(Automaton<BDD> originalAutomaton, PDLPred generatedFormula, Automaton<BDD> generatedAutomaton, HashSet<char> alphabet)
        {
            this.generatedFormula = generatedFormula;
            CharSetSolver solver = new CharSetSolver();

            DFAEditScript editScript = DFAEditDistance.GetDFAOptimalEdit(originalAutomaton, generatedAutomaton, alphabet, solver, 100, new System.Text.StringBuilder());
            if (editScript != null)
            {
                this.editDistance = editScript.script.Count;
            }
            else
            {
                this.editDistance = int.MaxValue;
            }
            this.densityDiff = DFADensity.GetDFADifferenceRatio(originalAutomaton, generatedAutomaton, alphabet, solver);
        }
    }
}