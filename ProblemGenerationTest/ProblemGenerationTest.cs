using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using AutomataPDL;
using Microsoft.Automata;

namespace ProblemGenerationTest
{
    [TestClass]
    public class ProblemGenerationTest
    {
        [TestMethod]
        public void Measure_1()
        {
            Testcase testcase = TestPredicateFactory.createTestFormula1();
            RunAndOutputBenchmarkSet(testcase);
        }

        [TestMethod]
        public void Measure_2()
        {
            Testcase testcase = TestPredicateFactory.createTestFormula2();
            RunAndOutputBenchmarkSet(testcase);
        }

        [TestMethod]
        public void Measure_3()
        {
            Testcase testcase = TestPredicateFactory.createTestFormula3();
            RunAndOutputBenchmarkSet(testcase);
        }

        [TestMethod]
        public void Measure_4()
        {
            Testcase testcase = TestPredicateFactory.createTestFormula4();
            RunAndOutputBenchmarkSet(testcase);
        }

        [TestMethod]
        public void Measure_5()
        {
            Testcase testcase = TestPredicateFactory.createTestFormula5();
            RunAndOutputBenchmarkSet(testcase);
        }

        [TestMethod]
        public void Measure_6()
        {
            Testcase testcase = TestPredicateFactory.createTestFormula6();
            RunAndOutputBenchmarkSet(testcase);
        }

        [TestMethod]
        public void Measure_7()
        {
            Testcase testcase = TestPredicateFactory.createTestFormula7();
            RunAndOutputBenchmarkSet(testcase);
        }

        [TestMethod]
        public void Measure_8()
        {
            Testcase testcase = TestPredicateFactory.createTestFormula8();
            RunAndOutputBenchmarkSet(testcase);
        }

        [TestMethod]
        public void Measure_9()
        {
            Testcase testcase = TestPredicateFactory.createTestFormula9();
            RunAndOutputBenchmarkSet(testcase);
        }

        [TestMethod]
        public void Measure_10()
        {
            Testcase testcase = TestPredicateFactory.createTestFormula10();
            RunAndOutputBenchmarkSet(testcase);
        }

        [TestMethod]
        public void Measure_11()
        {
            Testcase testcase = TestPredicateFactory.createTestFormula11();
            RunAndOutputBenchmarkSet(testcase);
        }

        [TestMethod]
        public void Measure_12()
        {
            Testcase testcase = TestPredicateFactory.createTestFormula12();
            RunAndOutputBenchmarkSet(testcase);
        }

        [TestMethod]
        public void Measure_13()
        {
            // Don't do this for now, testcase not yet finished
            //Testcase testcase = TestPredicateFactory.createTestFormula13();
            //RunAndOutputBenchmarkSet(testcase);
        }

        [TestMethod]
        public void Measure_14()
        {
            Testcase testcase = TestPredicateFactory.createTestFormula14();
            RunAndOutputBenchmarkSet(testcase);
        }

        [TestMethod]
        public void Measure_15()
        {
            Testcase testcase = TestPredicateFactory.createTestFormula15();
            RunAndOutputBenchmarkSet(testcase);
        }

        [TestMethod]
        public void Measure_16()
        {
            Testcase testcase = TestPredicateFactory.createTestFormula16();
            RunAndOutputBenchmarkSet(testcase);
        }

        [TestMethod]
        public void Measure_17()
        {
            Testcase testcase = TestPredicateFactory.createTestFormula17();
            RunAndOutputBenchmarkSet(testcase);
        }

        [TestMethod]
        public void Measure_18()
        {
            Testcase testcase = TestPredicateFactory.createTestFormula18();
            RunAndOutputBenchmarkSet(testcase);
        }

        [TestMethod]
        public void Measure_19()
        {
            Testcase testcase = TestPredicateFactory.createTestFormula19();
            RunAndOutputBenchmarkSet(testcase);
        }

        [TestMethod]
        public void Measure_20()
        {
            Testcase testcase = TestPredicateFactory.createTestFormula20();
            RunAndOutputBenchmarkSet(testcase);
        }

        [TestMethod]
        public void Measure_21()
        {
            Testcase testcase = TestPredicateFactory.createTestFormula21();
            RunAndOutputBenchmarkSet(testcase);
        }

        [TestMethod]
        public void Measure_22()
        {
            Testcase testcase = TestPredicateFactory.createTestFormula22();
            RunAndOutputBenchmarkSet(testcase);
        }

        public void RunAndOutputBenchmarkSet(Testcase testcase)
        {
            IDictionary<PDLPred, Measurement.SingleMeasurementResult> cache = new Dictionary<PDLPred, Measurement.SingleMeasurementResult>();
            IDictionary<Automaton<BDD>, Measurement.SingleMeasurementResult> automatonCache = new Dictionary<Automaton<BDD>, Measurement.SingleMeasurementResult>();
            foreach (VariableCache.ConstraintMode constraintMode in System.Enum.GetValues(typeof(VariableCache.ConstraintMode)))
            {
                foreach (PdlFilter.Filtermode filtermode in System.Enum.GetValues(typeof(PdlFilter.Filtermode)))
                {
                    Measurement.MeasurementResultSet resultSet = this.RunBenchmark(testcase, constraintMode, filtermode, cache, automatonCache);
                    using (System.IO.StreamWriter writer = new System.IO.StreamWriter(this.GetFilenameTest(testcase, constraintMode, filtermode)))
                    {
                        resultSet.Output(writer);
                    }
                }
            }
        }

        public Measurement.MeasurementResultSet RunBenchmark(Testcase testcase, VariableCache.ConstraintMode constraintMode, PdlFilter.Filtermode filtermode, IDictionary<PDLPred, Measurement.SingleMeasurementResult> cache, IDictionary<Automaton<BDD>, Measurement.SingleMeasurementResult> automatonCache)
        {
            ICollection<PDLPred> bareResults = new LinkedList<PDLPred>();

            System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();
            foreach(PDLPred result in ProblemGeneration.GeneratePDLWithEDn(testcase.language, testcase.alphabet, constraintMode, filtermode)) {
                bareResults.Add(result);
            }
            stopwatch.Stop();

            return new Measurement.MeasurementResultSet(testcase.language, bareResults, stopwatch.ElapsedMilliseconds, constraintMode, filtermode, new HashSet<char>(testcase.alphabet), cache, automatonCache);
        }

        private string GetFilenameTest(Testcase testcase, VariableCache.ConstraintMode constraintMode, PdlFilter.Filtermode filtermode)
        {
            return String.Format("C:/Users/alexander/Desktop/results/{0}.{1}.{2}.csv", testcase.id, constraintMode, filtermode);
        }

        public ICollection<PDLPred> FilterPredicates(PdlFilter filter, ICollection<PDLPred> candidates)
        {
            ICollection<PDLPred> returnValue = new HashSet<PDLPred>();
            foreach (PDLPred candidate in candidates)
            {
                if(filter.KeepPredicate(candidate)) {
                    returnValue.Add(candidate);
                }
            }
            return returnValue;
        }
    }
}
