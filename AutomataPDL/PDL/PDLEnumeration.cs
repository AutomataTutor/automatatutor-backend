using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Management;
using System.IO;

using System.Diagnostics;
using System.Threading;

using Microsoft.Automata;

namespace AutomataPDL
{
    public class PDLEnumerator
    {

        #region sizes for search
        int connectivesTotC = 0;
        int setCountTotC = 0;
        int substrTotC = 0;
        int maxWidthC = 0;
        int maxStrLengthC = 0;
        int highestNumC = 0;
        int maxEmptyStringC = 0;

        int connectivesTot = 0;
        int setCountTot = 0;
        int substrTot = 0;
        int maxWidth = 0;
        int maxStrLength = 0;
        int maxEmptyString = 0;
        int highestNum = 0; 
        #endregion

        public PDLEnumerator(){}

        #region TemporaryVars
        HashSet<int> cycleLengths;
        bool isCyclic;
        int[] modLengths;
        HashSet<string>[] allStrings;
        HashSet<string> loopStrs;
        HashSet<string>[] simpleModStrs;
        int numStates;
        HashSet<char> alph;
        Stopwatch timer; 
        #endregion

        public void InitializeSearchParameters(HashSet<char> alphabet, Automaton<BDD> dfa, CharSetSolver solver)
        {
            numStates = dfa.StateCount;
            alph = alphabet;

            #region Accessories vars
            connectivesTotC = 0;
            setCountTotC = 0;
            substrTotC = 0;
            maxWidthC = 0;
            maxStrLengthC = 0;
            highestNumC = 0;
            maxEmptyStringC = 0;
            #endregion

            #region Mod bounds computation
            cycleLengths = DFAUtilities.getCyclesLengths(dfa);
            isCyclic = cycleLengths.Count > 0;
            HashSet<int> modSet = new HashSet<int>();
            foreach (int num in cycleLengths)
                for (int i = 2; i <= num; i++)
                    if (num % i == 0)
                        modSet.Add(i);
            var modLengthsL = modSet.ToList();
            modLengthsL.Sort();
            modLengths = modLengthsL.ToArray();
            #endregion

            #region String bounds computation
            int allStringSize = dfa.StateCount >= 8 ? 5 : dfa.StateCount;
            allStrings = new HashSet<string>[allStringSize];

            for (int i = 0; i < allStringSize; i++)
            {
                allStrings[i] = new HashSet<string>();
                if (i == 0)
                    allStrings[0].Add("");
                else
                    foreach (var str in allStrings[i - 1])
                        foreach (var ch in alphabet)
                            allStrings[i].Add(str + ch);
            }
            #endregion

            #region Loop strings
            //
            loopStrs = DFAUtilities.getLoopingStrings(dfa, alphabet, solver);

            simpleModStrs = new HashSet<string>[dfa.StateCount + 1];
            for (int i = 0; i < dfa.StateCount + 1; i++)
                simpleModStrs[i] = new HashSet<string>();

            //if there is a loop of the form 'aaa', we should count 'a'% 3
            foreach (var loopStr in loopStrs)
                for (int stringSize = 1; stringSize <= loopStr.Length / 2; stringSize++)
                    if (stringSize < allStrings.Length)
                        foreach (var str in allStrings[stringSize])
                        {
                            int count = 0;
                            int strInd = 0;
                            for (int loopStrInd = 0; loopStrInd < loopStr.Length; loopStrInd++)
                                if (loopStr[loopStrInd] == str[strInd])
                                {
                                    strInd++;
                                    if (strInd == str.Length)
                                    {
                                        count++;
                                        strInd = 0;
                                    }
                                }

                            foreach (var modLength in modLengths)
                                if (count % modLength == 0)
                                    simpleModStrs[modLength].Add(str);
                        }


            //TODO Need to do something for union interseection 
            #endregion
        }

        public IEnumerable<PDLPred> SynthesizePDL(HashSet<char> alphabet, Automaton<BDD> dfa, CharSetSolver solver, StringBuilder sb, long timeout, int maxFormulas=1)
        {
            #region test variables
            int enumeratedPhis = 0;
            StringBuilder sb1 = new StringBuilder();
            int lim = 0;
            Stopwatch membershipTimer = new Stopwatch();
            Stopwatch equivTimer = new Stopwatch();
            timer = new Stopwatch();
            timer.Start();
            #endregion

            #region TestSets for equiv
            var mytests = DFAUtilities.MyHillTestGeneration(alphabet, dfa, solver);
            var posMN = mytests.First;
            var negMN = mytests.Second;
            var tests = DFAUtilities.GetTestSets(dfa, alphabet, solver);
            var positive = tests.First;
            var negative = tests.Second;
            foreach (var t in posMN)
                positive.Remove(t);
            foreach (var t in negMN)
                negative.Remove(t);
            #endregion

            #region Accessories vars
            var isSuperset = true;
            var isSubset = true;
            bool checkNeeded = false;
            string hash = "";
            HashSet<string> visited = new HashSet<string>();
            HashSet<string> newNodes = new HashSet<string>();
            List<PDLPred> supersetPhis = new List<PDLPred>();
            List<PDLPred> subsetPhis = new List<PDLPred>();
            #endregion

            InitializeSearchParameters(alphabet, dfa, solver);


            for (maxWidth = 3; true; maxWidth++)
            {
                int limit = (int)(Math.Sqrt(maxWidth));
                int limitH = (int)(Math.Sqrt(maxWidth + 5));
                maxStrLength = Math.Min(dfa.StateCount + 1, limit + 2);
                highestNum = Math.Min(limitH, dfa.StateCount - 1);
                substrTot = limit;
                connectivesTot = limit;
                setCountTot = limit;
                maxEmptyString = Math.Max(1, limit - 1);                

                newNodes = new HashSet<string>();

                foreach (var phi in EnumeratePDLpred(new HashSet<string>(), new HashSet<string>()))
                {
                    #region run for at most timeout
                    if (timer.ElapsedMilliseconds > timeout)
                    {
                        sb.AppendLine("| Timeout");
                        timer.Stop();
                        yield break;
                    }
                    #endregion

                    //Console.WriteLine(phi);

                    #region Check decider to avoid repetition
                    hash = string.Format(
                        "{0},{1},{2},{3},{4},{5},{6}",
                        maxWidthC, maxStrLengthC, highestNumC, substrTotC, connectivesTotC, setCountTotC, maxEmptyStringC);
                    checkNeeded = !(visited.Contains(hash));
                    #endregion

                    if (checkNeeded)
                    {
                        newNodes.Add(hash);

                        //sb1 = new StringBuilder();
                        //phi.ToString(sb1);
                        //sb1.AppendLine();
                        //phi.ToString(sb1);
                        lim++;

                        #region formula equivalence check

                        #region Membership test
                        membershipTimer.Start();
                        isSuperset = CorrectOnPosSet(phi, posMN);
                        isSubset = CorrectOnNegSet(phi, negMN);
                        membershipTimer.Stop();
                        #endregion

                        if (isSuperset && isSubset)
                        {
                            membershipTimer.Start();
                            if (CorrectOnPosSet(phi, positive))
                            {
                                if (CorrectOnNegSet(phi, negative))
                                {
                                    membershipTimer.Stop();
                                    equivTimer.Start();
                                    var phidfa = phi.GetDFA(alphabet, solver);
                                    if (phidfa != null && phidfa.IsEquivalentWith(dfa, solver))
                                    {
                                        equivTimer.Stop();
                                        timer.Stop();

                                        sb.Append("| ");
                                        phi.ToString(sb);
                                        sb.AppendLine();
                                        phi.ToString(sb);
                                        sb.AppendLine(); sb.AppendLine("|");
                                        sb.AppendLine(string.Format("| elapsed time:    \t {0} ms", timer.ElapsedMilliseconds));
                                        sb.AppendLine(string.Format("| equivalence cost:\t {0} ms", equivTimer.ElapsedMilliseconds));
                                        sb.AppendLine(string.Format("| membership cost: \t {0} ms", membershipTimer.ElapsedMilliseconds));
                                        sb.AppendLine(string.Format("| attempts:        \t {0}", lim));
                                        isSubset = false;
                                        isSuperset = false;
                                        yield return phi;
                                        enumeratedPhis++;
                                        if (enumeratedPhis > maxFormulas)
                                            yield break;

                                        timer.Start();
                                    }
                                    else
                                    {
                                        equivTimer.Stop();
                                        //Console.WriteLine("used dfa with: {0}", phi);
                                    }
                                }
                                else
                                {
                                    membershipTimer.Stop();
                                    isSubset = false;
                                }
                            }
                            else
                            {
                                membershipTimer.Stop();
                                isSuperset = false;
                            }
                        }

                        #region Union generation with memoization
                        membershipTimer.Start();
                        if (!(phi is PDLFalse) && isSubset && CorrectOnNegSet(phi, negative))
                        {
                            membershipTimer.Stop();
                            foreach (var psi in subsetPhis)
                            {
                                var union = (phi.CompareTo(psi) > 0) ? new PDLOr(phi, psi) : new PDLOr(psi, phi);

                                if (timer.ElapsedMilliseconds > timeout)
                                {
                                    sb.AppendLine("| Timeout");
                                    timer.Stop();
                                    yield break;
                                }

                                membershipTimer.Start();
                                if (CorrectOnPosSet(union, posMN) && CorrectOnPosSet(union, positive))
                                {
                                    membershipTimer.Stop();

                                    equivTimer.Start();
                                    var uniondfa = union.GetDFA(alphabet, solver);
                                    if (uniondfa != null && uniondfa.IsEquivalentWith(dfa, solver))
                                    {
                                        equivTimer.Stop();
                                        timer.Stop();
                                        sb.Append("| ");
                                        union.ToString(sb);
                                        sb.AppendLine();
                                        union.ToString(sb);
                                        sb.AppendLine(); sb.AppendLine("| ");
                                        sb.AppendLine(string.Format("| elapsed time:    \t {0} ms", timer.ElapsedMilliseconds));
                                        sb.AppendLine(string.Format("| equivalence cost:\t {0} ms", equivTimer.ElapsedMilliseconds));
                                        sb.AppendLine(string.Format("| membership cost: \t {0} ms", membershipTimer.ElapsedMilliseconds));
                                        sb.AppendLine(string.Format("| attempts:        \t {0}", lim));

                                        yield return union;
                                        enumeratedPhis++;
                                        if (enumeratedPhis > maxFormulas)
                                            yield break;

                                        timer.Start();
                                    }
                                    else
                                    {
                                        equivTimer.Stop();
                                        //Console.WriteLine("used dfa with union: {0}", union);
                                    }
                                }
                            }
                            subsetPhis.Add(phi);
                        }
                        else
                            membershipTimer.Stop();
                        #endregion

                        #region Intersection generation with memoization
                        membershipTimer.Start();
                        if (!(phi is PDLTrue) && isSuperset && CorrectOnPosSet(phi, positive))
                        {
                            membershipTimer.Stop();
                            foreach (var psi in supersetPhis)
                            {
                                if (timer.ElapsedMilliseconds > timeout)
                                {
                                    sb.AppendLine("| Timeout");
                                    timer.Stop();
                                    yield break;
                                }

                                var intersection = (phi.CompareTo(psi) > 0) ? new PDLAnd(phi, psi) : new PDLAnd(psi, phi);


                                membershipTimer.Start();
                                //should check if superset still??
                                if (CorrectOnNegSet(intersection, negMN) && CorrectOnNegSet(intersection, negative))
                                {
                                    membershipTimer.Stop();
                                    equivTimer.Start();
                                    var intersectionDFA = intersection.GetDFA(alphabet, solver);
                                    if (intersectionDFA != null && intersectionDFA.IsEquivalentWith(dfa, solver))
                                    {
                                        equivTimer.Stop();
                                        timer.Stop();
                                        sb.Append("| ");
                                        intersection.ToString(sb);
                                        sb.AppendLine();
                                        intersection.ToString(sb);
                                        sb.AppendLine(); sb.AppendLine("|");
                                        sb.AppendLine(string.Format("| elapsed time:    \t {0} ms", timer.ElapsedMilliseconds));
                                        sb.AppendLine(string.Format("| equivalence cost:\t {0} ms", equivTimer.ElapsedMilliseconds));
                                        sb.AppendLine(string.Format("| membership cost: \t {0} ms", membershipTimer.ElapsedMilliseconds));
                                        sb.AppendLine(string.Format("| attempts:        \t {0}", lim));

                                        yield return intersection;
                                        enumeratedPhis++;
                                        if (enumeratedPhis > maxFormulas)
                                            yield break;

                                        timer.Start();
                                    }
                                    else
                                    {
                                        equivTimer.Stop();
                                        //Console.WriteLine("used dfa with inters: {0}",intersection);
                                    }
                                }
                                else
                                    membershipTimer.Stop();
                            }
                            supersetPhis.Add(phi);
                        }
                        else
                            membershipTimer.Stop();
                        #endregion

                        #endregion
                    }
                }
                visited = new HashSet<string>(visited.Union(newNodes));
            }
        }
        

        public IEnumerable<Pair<PDLPred,double>> SynthesizeUnderapproximationPDL(HashSet<char> alphabet, Automaton<BDD> dfa, CharSetSolver solver, StringBuilder sb, long timeout)
        {
            #region TestSets for equiv
            var mytests = DFAUtilities.MyHillTestGeneration(alphabet, dfa, solver);
            var posMN = mytests.First;
            var negMN = mytests.Second;
            var tests = DFAUtilities.GetTestSets(dfa, alphabet, solver);
            var positive = tests.First;
            var negative = tests.Second;
            foreach (var t in posMN)
                positive.Remove(t);
            foreach (var t in negMN)
                negative.Remove(t);
            #endregion

            #region Accessory variables
            bool checkNeeded = false;
            string hash = "";
            HashSet<string> visited = new HashSet<string>();
            HashSet<string> newNodes = new HashSet<string>();
            List<Pair<PDLPred, double>> subsetPhis = new List<Pair<PDLPred, double>>();
            timer = new Stopwatch();
            timer.Start();
            #endregion

            InitializeSearchParameters(alphabet, dfa, solver);


            for (maxWidth = 3; true; maxWidth++)
            {
                int limit = (int)(Math.Sqrt(maxWidth));
                int limitH = (int)(Math.Sqrt(maxWidth + 5));
                maxStrLength = Math.Min(dfa.StateCount + 1, limit + 2);
                highestNum = Math.Min(limitH, dfa.StateCount - 1);
                substrTot = limit;
                connectivesTot = limit;
                setCountTot = limit;
                maxEmptyString = Math.Max(1, limit - 1);

                newNodes = new HashSet<string>();

                foreach (var phi in EnumeratePDLpred(new HashSet<string>(), new HashSet<string>()))
                {
                    #region run for at most timeout
                    if (timer.ElapsedMilliseconds > timeout)
                    {
                        subsetPhis.Sort(ComparePairsPhiDoubleByDouble);
                        timer.Stop();
                        return subsetPhis;
                    }
                    #endregion

                    #region Check decider to avoid repetition
                    hash = string.Format(
                        "{0},{1},{2},{3},{4},{5},{6}",
                        maxWidthC, maxStrLengthC, highestNumC, substrTotC, connectivesTotC, setCountTotC, maxEmptyStringC);
                    checkNeeded = !(visited.Contains(hash));
                    #endregion

                    if (checkNeeded)
                    {
                        newNodes.Add(hash);

                        #region add it to set of results if it is a subset
                        if (CorrectOnNegSet(phi, negMN) && CorrectOnNegSet(phi, negative))
                        {
                            var phiDfa = phi.GetDFA(alphabet, solver);
                            if (!phiDfa.IsEmpty)
                                if (phiDfa.Minus(dfa, solver).IsEmpty)
                                    subsetPhis.Add(
                                        new Pair<PDLPred, double>(
                                            phi,
                                            (double)phi.GetFormulaSize() / DFADensity.GetDFARatio(dfa, phiDfa, alphabet, solver, true)
                                        )
                                    );
                        }
                        #endregion
                    }
                }
                visited = new HashSet<string>(visited.Union(newNodes));
            }

        }

        #region Membership tests
        //True if phi does not accepts all the strings in testset
        private static bool CorrectOnNegSet(PDLPred phi, IEnumerable<string> testSet)
        {
            foreach (var test in testSet)
                if (phi.Eval(test, new Dictionary<string, int>()))
                {
                    return false;
                }
            return true;
        }

        //True if phi accepts all the strings in testset
        private static bool CorrectOnPosSet(PDLPred phi, IEnumerable<string> testSet)
        {
            foreach (var test in testSet)
                if (!phi.Eval(test, new Dictionary<string, int>()))
                {
                    return false;
                }
            return true;
        }

        //Return what percentage of testSet phi accepts
        private static double PercentagePosSet(PDLPred phi, IEnumerable<string> testSet)
        {            
            double correct =0;
            double total = 0;
            foreach (var test in testSet)
            {
                total++;
                if (!phi.Eval(test, new Dictionary<string, int>()))
                    correct++;
            }
            return total==0?0:correct/total;
        } 
        #endregion


        #region PDL enumeration
        //Positions enumerator
        private IEnumerable<PDLPos> EnumeratePDLpos(ICollection<string> fov, ICollection<string> sov)
        {
            if (maxWidthC == maxWidth)
                yield break;

            maxWidthC++;

            yield return new PDLFirst();
            yield return new PDLLast();

            foreach (var v in fov)
                yield return new PDLPosVar(v);

            foreach (var pos1 in EnumeratePDLpos(fov, sov))
            {
                if (!(pos1 is PDLLast || pos1 is PDLPredecessor))              //Remove: next(last); next(prev A)
                    yield return new PDLSuccessor(pos1);
                if (!(pos1 is PDLFirst || pos1 is PDLSuccessor))             //Remove: prev(first); prev(next A)
                    yield return new PDLPredecessor(pos1);
            }

            #region PDLFirstOcc PDLLastOcc
            if (substrTotC < substrTot)
            {
                substrTotC++;
                int remainingStringSpace = maxStrLength - maxStrLengthC;    //Bound of max strings used in expr
                for (int strLength = 1; strLength <= remainingStringSpace; strLength++)
                    if (strLength < allStrings.Length)                  //Remove strings bigger than max length allowed by dfa
                    {
                        maxStrLengthC += strLength;
                        foreach (var s in allStrings[strLength])
                        {       //Use one of the allowed strings
                            yield return new PDLFirstOcc(s);
                            yield return new PDLLastOcc(s);
                        }
                        maxStrLengthC -= strLength;
                    }
                    else
                        break;

                substrTotC--;
            }
            #endregion

            maxWidthC--;
        }

        //Sets enumerator
        private IEnumerable<PDLSet> EnumeratePDLset(ICollection<string> fov, ICollection<string> sov)
        {
            if (maxWidthC == maxWidth)
                yield break;

            maxWidthC++;

            #region PDLallPos
            yield return new PDLAllPos();
            #endregion

            #region PDLindicesOf
            if (substrTotC < substrTot)
            {
                substrTotC++;
                int remainingStringSpace = maxStrLength - maxStrLengthC;    //Bound of max strings used in expr
                for (int strLength = 1; strLength <= remainingStringSpace; strLength++)
                    if (strLength < allStrings.Length)                  //Remove strings bigger than max length allowed by dfa
                    {
                        maxStrLengthC += strLength;
                        foreach (var s in allStrings[strLength])        //Use one of the allowed strings
                            yield return new PDLIndicesOf(s);
                        maxStrLengthC -= strLength;
                    }
                    else
                        break;

                substrTotC--;
            }
            #endregion

            #region PDLallPosAfter  PDLallPosFrom  PDLallPosBefore  PDLallPosUpto
            int size;
            foreach (var pos1 in EnumeratePDLpos(fov, sov))
            {
                if (!(IsNumericalFromLast(pos1, out size)))             //Remove: allPosFrom(prev prev prev last)  allPosAfter(prev prev prev last) 
                {
                    bool fine = true;
                    var v1 = pos1 as PDLFirstOcc;                       //Remove: allPosAfter (firstocc "aa")
                    if (v1 != null && v1.str.Length > 1)
                        fine = false;

                    var v2 = pos1 as PDLLastOcc;                       //Remove: allPosAfter (lastocc "aa")
                    if (v2 != null && v2.str.Length > 1)
                        fine = false;

                    if (fine)
                    {
                        if (!(pos1 is PDLPredecessor))                             //Remove: allPosAfter(prev A) since equiv to allPosFrom(A)
                            yield return new PDLAllPosAfter(pos1);
                        if (!(pos1 is PDLFirst || pos1 is PDLSuccessor))         //Remove: allPosFrom(first); allPosFrom(next A) since equiv to allPosAfter(A)
                            yield return new PDLAllPosFrom(pos1);
                    }
                }
                if (!(IsNumericalFromFirst(pos1, out size)))            //Remove: allPosUpto(next next first)  allPosBefore(next next first) 
                {
                    if (!(pos1 is PDLSuccessor))                             //Remove: allPosBefore(next A) since equiv to allPosUpto(A)
                        yield return new PDLAllPosBefore(pos1);
                    if (!(pos1 is PDLLast || pos1 is PDLPredecessor))          //Remove: allPosUpto(last); allPosUpto(prev A) since equiv to allPosBefore(A)
                        if (!IsNumericalFromFirstLastOcc(pos1, out size)) //Remove: allPosUpto(firstOcc, LastOcc, Firstooc+1...)
                            yield return new PDLAllPosUpto(pos1);
                }
            }
            #endregion

            #region PDLunion PDLintersect
            foreach (var set1 in EnumeratePDLset(fov, sov))
                if (!(set1 is PDLAllPos))
                {
                    var s1 = set1 as PDLIndicesOf;
                    foreach (var set2 in EnumeratePDLset(fov, sov))
                        if (!(set2 is PDLAllPos))
                            if (set1.CompareTo(set2) < 0)
                            {
                                bool intersectFine = true, unionFine = true;
                                var s2 = set2 as PDLIndicesOf;
                                if (s1 != null && s2 != null)
                                {
                                    intersectFine = false;
                                    unionFine = !(s1.str.StartsWith(s2.str) || s2.str.StartsWith(s1.str));
                                }

                                if (unionFine)
                                    yield return new PDLUnion(set1, set2);
                                if (intersectFine)
                                    yield return new PDLIntersect(set1, set2);
                            }
                }
            #endregion

            #region PDLpredSet
            var fovcl = new List<string>();
            string newVarName = "x" + fov.Count;
            //fovcl should only be new x and sov should be empty. Weird semantics otherwise
            fovcl.Add(newVarName);

            foreach (var pred1 in EnumeratePDLpred(fovcl, new List<string>()))
            {
                if (pred1.ContainsVar(newVarName))           //Remove: predSet(x, A) such that x not in A
                {
                    //TODO Why pruning all of these
                    if (!(pred1 is PDLPosEq || pred1 is PDLPosLe || pred1 is PDLAtPos || pred1 is PDLAtSet ||
                        pred1 is PDLIntEq || pred1 is PDLIntLeq || pred1 is PDLIntGeq ||
                        pred1 is PDLModSetLe))
                    {
                        bool fine = true;
                        var pr = pred1 as PDLBelongs;
                        if (pr != null)
                        {
                            var prs = pr.set as PDLIndicesOf;
                            if (prs != null)
                            {
                                fine = false;
                            }
                        }
                        if (fine)
                            yield return new PDLPredSet(newVarName, pred1);
                    }
                }
            }
            #endregion

            maxWidthC--;
        }

        //Predicates enumerator
        internal IEnumerable<PDLPred> EnumeratePDLpred(ICollection<string> fov, ICollection<string> sov)
        {
            if (maxWidthC == maxWidth)
                yield break;

            maxWidthC++;

            #region PDLEmptyString
            if (maxEmptyStringC < maxEmptyString)
            {
                maxEmptyStringC++;
                yield return new PDLEmptyString();
                maxEmptyStringC--;
            }
            #endregion

            #region PDLTrue PDLFalse
            if (maxWidthC == 1)
            {
                yield return new PDLTrue();
                yield return new PDLFalse();
            }
            #endregion

            #region PDLStartsWith PDLEndsWith
            substrTotC++;                               //Consider only strings up to the remaining length

            for (int strLength = 1; strLength <= maxStrLength - maxStrLengthC; strLength++)
                if (strLength < allStrings.Length)
                {
                    maxStrLengthC += strLength;
                    foreach (var str in allStrings[strLength])
                    {
                        yield return new PDLStartsWith(str);
                        yield return new PDLEndsWith(str);
                    }
                    maxStrLengthC -= strLength;
                }
                else
                    break;

            substrTotC--;
            #endregion


            //#region PDLeqString
            //if (maxWidthC <= 2)
            //    if (substrTotC < substrTot)
            //    {
            //        substrTotC++;
            //        int remainingStringSpace = maxStrLength - maxStrLengthC;    //Bound of max strings used in expr
            //        for (int strLength = 1; strLength <= remainingStringSpace; strLength++)
            //            if (strLength < allStrings.Length)                  //Remove strings bigger than max length allowed by dfa
            //            {
            //                maxStrLengthC += strLength;
            //                foreach (var s in allStrings[strLength])        //Use one of the allowed strings
            //                    yield return new PDLeqString(s);
            //                maxStrLengthC -= strLength;
            //            }
            //            else
            //                break;
            //        substrTotC--;
            //    }
            //#endregion            

            #region Quantifiers
            var fovc = fov.ToArray();
            var fovcl = fovc.ToList();
            string newVarName = "x" + fov.Count;
            fovcl.Add(newVarName);

            foreach (var pred1 in EnumeratePDLpred(fovcl, sov))
            {
                if (pred1.ContainsVar(newVarName))                              //Remove: ex x. A, such that x is not in A
                {
                    if (!(pred1 is PDLPosEq || pred1 is PDLPosLe ||             //Remove: ex1 x. A=B; all1 x. A=B; ex1 x. A<B; all1 x. A<B
                            pred1 is PDLIntEq || pred1 is PDLIntLeq ||          //Remove: ex1 x. |S|=i; all1 x. |S|=i; ex1 x. |S|<=i; all1 x. |S|<=i 
                                pred1 is PDLModSetEq || pred1 is PDLModSetLe))  //Remove: ex1 x. |S|%n =m; all1 x. |S|%n =m; ex1 x. |S|%n<=m; all1 x. |S|%n<=m 
                    {
                        bool fineEx = true;
                        bool fineAll = true;
                        var pr = pred1 as PDLBelongs;
                        if (pr != null)
                        {
                            var prs = pr.set as PDLIndicesOf;
                            if (prs != null)
                            {
                                fineAll = false;                                //Remove: all1 x. x belTo (indOF s) 
                                fineEx = false;                                 //Remove: ex1 x. x belTo (indOF s)
                            }
                        }

                        #region PDLExistsFO
                        if (fineEx)
                            yield return new PDLExistsFO(newVarName, pred1);
                        #endregion

                        #region PDLForallFO
                        if (fineAll)
                            yield return new PDLForallFO(newVarName, pred1);
                        #endregion
                    }
                }
            }

            //For now SO not used in RHS
            //var sovc = sov.ToArray();
            //var sovcl = sovc.ToList();
            //newVarName = "X" + sov.Count;
            //sovcl.Add(newVarName);
            //foreach (var pred1 in EnumeratePDLpred(alphabet,
            //    maxWidth, andOrNum, setCountTot, substrTot, maxStrLength, numbers, isCyclic, numStates,
            //    modLengths, allStrings, loopStr,
            //    fov, sovcl))
            //    {
            //    if (pred1.ContainsVar(newVarName))
            //    {
            //        yield return new PDLForallSO(newVarName, pred1);
            //        yield return new PDLExistsSO(newVarName, pred1);
            //    }
            //} 
            #endregion

            foreach (var pos1 in EnumeratePDLpos(fov, sov))
            {
                //uses pos1
                #region PDLatPos
                if (!(pos1 is PDLFirst || pos1 is PDLLast || pos1 is PDLFirstOcc || pos1 is PDLLastOcc))    //Remove: 'a' @ first, 'a' @ last, 'a' @ firstocc, 'a' @ lastocc
                {
                    bool fine = true;
                    var v1 = pos1 as PDLPredecessor;
                    if (v1 != null)
                        if (v1.pos is PDLLastOcc || v1.pos is PDLFirstOcc)
                            fine = false;

                    var v2 = pos1 as PDLSuccessor;
                    if (v2 != null)
                        if (v2.pos is PDLLastOcc || v2.pos is PDLFirstOcc)
                            fine = false;

                    if (fine && maxStrLength > maxStrLengthC)
                    {
                        maxStrLengthC++;
                        foreach (var el in alph)
                            yield return new PDLAtPos(el, pos1);
                        maxStrLengthC--;
                    }
                }
                #endregion

                //uses pos1 pos2
                #region PDLPosEq PDLPosLe
                foreach (var pos2 in EnumeratePDLpos(fov, sov))
                    if (!(pos1 is PDLSuccessor && pos2 is PDLSuccessor) &&                            //Remove: A+1 = A+1; A+1 < A+1
                            !(pos1 is PDLPredecessor && pos2 is PDLPredecessor) &&                        //Remove: A-1 = A-1; A-1 < A-1
                                !(pos1 is PDLFirst && pos2 is PDLSuccessor) &&                   //Remove: first = A+1; first < A+1
                                    !(pos2 is PDLFirst && pos1 is PDLSuccessor) &&               //Remove: A+1 = first; A+1 < first;
                                        !(pos1 is PDLLast && pos2 is PDLPredecessor) &&            //Remove: last = A-1; last < A-1;
                                            !(pos2 is PDLLast && pos1 is PDLPredecessor))          //Remove: A-1 = last; A-1 < last;
                    {
                        var difference = pos1.CompareTo(pos2);
                        if (difference != 0)                                        //Remove: A = A; A < A;
                        {
                            int n1, n2;
                            if (difference < 0)                                     //Either A = B or B = A;
                                if (!(pos1 is PDLFirst && pos2 is PDLLast))
                                    if (!(SyntacticEquivIsTrivial(pos1, pos2)))         //Remove: A = A+n; A-n = A;
                                        if (!(IsNumericalFromLast(pos1, out n1) && IsNumericalFromFirst(pos2, out n2) && (n1 != 1 || n2 != 1))) //Remove last = first +1
                                            if (!(IsNumericalFromFirstLastOcc(pos1, out n1)))
                                                if (!(IsNumericalFromFirstLastOcc(pos2, out n1)))
                                                    yield return new PDLPosEq(pos1, pos2);
                            if (!(pos1 is PDLFirst || pos1 is PDLLast ||            //Remove: first < A; last < A;
                                    pos2 is PDLFirst || pos2 is PDLLast))           //Remove: A < first; A < last;                           
                                if (!(SyntacticDisequivIsTrivial(pos1, pos2)))      //Remove: A < A+n; A-n < A;
                                    if (!(IsNumericalFromFirst(pos2, out n2) && n2 == 2))      //Remove: A < first+1
                                        if (!(IsNumericalFromLast(pos1, out n1) && n1 == 2))   //Remove: last - 1 < A
                                            if (!(IsNumericalFromFirstLastOcc(pos1, out n1)))
                                                if (!(IsNumericalFromFirstLastOcc(pos2, out n1)))
                                                    yield return new PDLPosLe(pos1, pos2);
                        }
                    }
                #endregion

                //uses pos1 set1
                #region PDLbelongs
                int size;
                foreach (var set1 in EnumeratePDLset(fov, sov))
                {
                    int length = 0;
                    var vset = set1 as PDLIndicesOf;
                    if (vset != null)
                        length = vset.str.Length;

                    if (!(pos1 is PDLLast || pos1 is PDLFirst || set1 is PDLAllPos ||       //Remove: first belTo A; last belTo A; A belTo allpos
                            set1 is PDLAllPosAfter || set1 is PDLAllPosBefore ||            //Remove: A belTo allAfter B; A belTo allBefore B same as <
                                set1 is PDLAllPosFrom || set1 is PDLAllPosUpto))            //Remove: A belTo allFrom B; A belTo allUpto B   same as <
                        if (!(length == 1))                                                 //Remove: A belTo indOf('a')    same as: a @ A
                            if (!(IsNumericalFromLast(pos1, out size) && size <= length))   //Remove: prev(last) belTo indOf('ab')  same as: endsWith 'ab'
                                yield return new PDLBelongs(pos1, set1);
                }
                #endregion
            }

            int highestNumTmp = 0;
            foreach (var set1 in EnumeratePDLset(fov, sov))
            {
                #region PDLatSet
                if (!(set1 is PDLIndicesOf))                         //Remove:  a @ (indicesOf s)
                    if (maxStrLength > maxStrLengthC)
                    {
                        maxStrLengthC++;
                        foreach (var el in alph)
                            if (!cAtSetIsTrivial(el, set1))           //Remove:  a @ ...U...U..inters (indicesOf s)
                                yield return new PDLAtSet(el, set1);
                        maxStrLengthC--;
                    }
                #endregion

                #region PDLintEq PDLintLeq PDLModSetEq PDLModSetLe
                if (setCountTotC < setCountTot)
                {
                    setCountTotC++;

                    int setSize = 1;                                                        //setSize is the size of the string in PDLindices of and 1 otherwise
                    int strSize = 0;                                                        //strSize is the size of the string in PDLindices of and 0 otherwise
                    string str = null;
                    var isLoopString = false;
                    if (set1 is PDLIndicesOf)
                    {
                        var s = set1 as PDLIndicesOf;
                        str = s.str;
                        setSize = s.str.Length;
                        strSize = s.str.Length;
                        isLoopString = loopStrs.Contains(s.str);
                    }

                    #region PDLintEq PDLintLeq
                    if (!(set1 is PDLAllPosFrom || set1 is PDLAllPosUpto ||                         //Remove: |allFrom A|=i; |allFrom A|<=i; |allUpto A|=i; |allUpto A|<=i; 
                                        set1 is PDLAllPosAfter || set1 is PDLAllPosBefore))                     //Remove: |allAfter A|=i; |allAfter A|<=i; |allBefore A|=i; |allBefore A|<=i; 
                    {
                        if (!(strSize == 1 || set1 is PDLAllPos))                                                //Remove: |indOf 'a'|=0; same as not(ex1 x. a @ x);
                            yield return new PDLIntEq(set1, 0);

                        if (highestNum >= 1 && setSize < numStates)                         //Don't count more than the (numStates-1)
                        {
                            highestNumTmp = highestNumC;
                            highestNumC = Math.Max(highestNumTmp, 1);

                            yield return new PDLIntEq(set1, 1);
                            yield return new PDLIntLeq(set1, 1);
                            //Remove indof 'a'>=1
                            PDLIndicesOf cast = set1 as PDLIndicesOf;
                            if(cast==null || cast.str.Length!=1)
                                yield return new PDLIntGeq(set1, 1);

                            if (highestNum >= 2 && (setSize * 2 < numStates))               //Don't count something twice if its size is more than (numStates-1)/2
                            {
                                highestNumC = Math.Max(highestNumTmp, 2);

                                if (!(set1 is PDLAllPos))
                                    yield return new PDLIntEq(set1, 2);
                                yield return new PDLIntLeq(set1, 2);
                                yield return new PDLIntGeq(set1, 2);

                                if (highestNum >= 3 && (setSize * 3 < numStates))           //Don't count something thrice if its size is more than (numStates-1)/3
                                {
                                    highestNumC = Math.Max(highestNumTmp, 3);

                                    if (!(set1 is PDLAllPos))
                                        yield return new PDLIntEq(set1, 3);
                                    yield return new PDLIntLeq(set1, 3);
                                    yield return new PDLIntGeq(set1, 3);

                                    // TODO Decide what to do for greater DFA construction not supported
                                }
                            }

                            highestNumC = highestNumTmp;
                        }
                    }
                    #endregion

                    #region PDLModSetEq PDLModSetLe
                    if (!(isLoopString))                                                         //Avoid strings that form a loop
                    {
                        for (int i = 0; i <= Math.Min(highestNum, modLengths.Length - 1); i++)
                        {
                            var sizemod = modLengths[i];
                            if (!(set1 is PDLIndicesOf) || simpleModStrs[sizemod].Contains(str))    //Remove: |indOf s| % n = m; if s does not appear k times (k divides m) in a cycle
                            {
                                highestNumTmp = highestNumC;
                                highestNumC = Math.Max(highestNumC, i);
                                for (int j = 0; j < sizemod; j++)
                                {
                                    if (!(set1 is PDLAllPosAfter || set1 is PDLAllPosBefore))       //Remove: |allAfter S| % n = m; |allBefore S| % n = m
                                        yield return new PDLModSetEq(set1, sizemod, j);

                                    if (j > 1)
                                        if (!(set1 is PDLAllPosAfter || set1 is PDLAllPosBefore ||    //Remove: |allAfter S| % n < n-1; |allBefore S| % n < n-1
                                                (sizemod - j <= 1)))
                                            yield return new PDLModSetLe(set1, sizemod, j);
                                }
                                highestNumC = highestNumTmp;
                            }

                        }
                    }
                    #endregion

                    setCountTotC--;
                }
                #endregion
            }

            #region Boolean connectives
            if (connectivesTotC < connectivesTot)
            {
                connectivesTotC++;
                foreach (var pred1 in EnumeratePDLpred(fov, sov))
                {
                    if (!(pred1 is PDLEmptyString)) //Emptystring doesn't go well with connectives (hard to understand)
                    {
                        #region Binary Connectives
                        foreach (var pred2 in EnumeratePDLpred(fov, sov))
                        {
                            if (!(pred1 is PDLEmptyString)) //Emptystring doesn't go well with connectives (hard to understand)
                            {
                                if (!(pred1 is PDLNot && pred2 is PDLNot))                              //Remove:  not(A) connective not(B)
                                {
                                    #region reductions based on endsWith startsWith
                                    bool andFine = true, orFine = true, ifFine = true, iffFine = true;
                                    if (pred1 is PDLEmptyString || pred2 is PDLEmptyString)
                                    {
                                        andFine = false;                                                //Remove emptyString and A
                                        ifFine = false;                                                 //Remove emptyString --> A; A --> emptyString
                                        iffFine = false;                                                //Remove emptyString <-> A
                                    }

                                    var difference = pred1.CompareTo(pred2);
                                    if (pred1 is PDLStartsWith && pred2 is PDLStartsWith)
                                    {
                                        andFine = false;                                                //Remove startsWith A and startsWith B
                                        ifFine = false;                                                 //Remove startsWith A if startsWith B
                                        iffFine = false;                                                //Remove startsWith A iff startsWith B
                                        var p1 = pred1 as PDLStartsWith;
                                        var p2 = pred2 as PDLStartsWith;
                                        if (p1.str.Length <= p2.str.Length)
                                        {
                                            if (p1.str == p2.str.Substring(0, p1.str.Length))
                                                orFine = false;                                         //Remove startsWith s or startsWith ss'
                                        }
                                        else
                                        {
                                            if (p2.str == p1.str.Substring(0, p2.str.Length))
                                                orFine = false;                                         //Remove startsWith ss' or startsWith s
                                        }

                                    }
                                    if (pred1 is PDLEndsWith && pred2 is PDLEndsWith)
                                    {
                                        andFine = false;                                                //Remove endsWith A and startsWith B
                                        ifFine = false;                                                 //Remove endsWith A if startsWith B
                                        iffFine = false;                                                //Remove endsWith A iff startsWith B
                                        var p1 = pred1 as PDLEndsWith;
                                        var p2 = pred2 as PDLEndsWith;
                                        if (p1.str.Length <= p2.str.Length)
                                        {
                                            if (p1.str == p2.str.Substring(p2.str.Length - p1.str.Length, p1.str.Length))
                                                orFine = false;                                         //Remove endsWith s or endsWith s's
                                        }
                                        else
                                        {
                                            if (p2.str == p1.str.Substring(p1.str.Length - p2.str.Length, p2.str.Length))
                                                orFine = false;                                         //Remove endsWith s's or endsWith s
                                        }
                                    }
                                    #endregion

                                    if (difference != 0)                                                  //Remove:  A connective A
                                    {
                                        if (difference < 0)                                               //Do either (A connective B), or (B connective A)
                                        {
                                            if (andFine)
                                                yield return new PDLAnd(pred1, pred2);

                                            if (orFine && !(pred1 is PDLNot) && !(pred2 is PDLNot))      //Remove: A or not(B); not(A) or B
                                                yield return new PDLOr(pred1, pred2);
                                            if (iffFine)
                                                yield return new PDLIff(pred1, pred2);
                                        }

                                        if (ifFine && !(pred1 is PDLNot) && !(pred2 is PDLNot))         //Remove: Not(A) -> B, A -> Not(B)
                                            yield return new PDLIf(pred1, pred2);

                                    }
                                }
                            }
                        }
                    }
                    #endregion

                    #region PDLnot
                    if (!(pred1 is PDLNot || pred1 is PDLIf ||                  //Remove: not(Not A);  not(A -> B)
                            pred1 is PDLIntLeq || pred1 is PDLIntLe ||          //Remove: not(|S| <= i); not(|S| < i)
                            pred1 is PDLIntGeq || pred1 is PDLIntGe))           //Remove: not(|S| >= i); not(|S| > i)        
                    {
                        bool fine = true;

                        var pr11 = pred1 as PDLForallFO;                        //remove: not forall (A);  not exists (A)
                        //var pr12 = pred1 as PDLForallSO;
                        var pr13 = pred1 as PDLExistsFO;
                        //var pr14 = pred1 as PDLExistsSO;
                        if ((pr11 != null) ||
                            //(pr12 != null) ||
                            //(pr14 != null) ||
                            (pr13 != null))
                            fine = false;

                        if (fine)
                        {
                            var pr1 = pred1 as PDLModSetEq;                     //Remove: not(|S| % 2 = i);  not(|S| % m = m-1)
                            if (pr1 != null)
                                if (pr1.m == 2 || pr1.m == pr1.n + 1)
                                    fine = false;

                            if (fine)
                            {
                                var pr2 = pred1 as PDLModSetLe;                 //Remove: not(|S| % m < m-1)
                                if (pr2 != null)
                                    if (pr2.m == pr2.n + 1)
                                        fine = false;

                                if (fine)
                                {
                                    var pr3 = pred1 as PDLPosEq;
                                    if (pr3 != null)
                                        if (pr3.pos1 is PDLFirst || pr3.pos2 is PDLFirst ||     //Remove: not(first=A); not(A=first)
                                                pr3.pos2 is PDLFirst || pr3.pos2 is PDLFirst)   //Remove: not(A=last); not(last=A)
                                            fine = false;

                                    if (fine)
                                        yield return new PDLNot(pred1);
                                }
                            }
                        }
                    }
                    #endregion
                }
                connectivesTotC--;
            }
            #endregion

            maxWidthC--;
        } 
        #endregion


        #region Accessories
        //return true if the predicate c@S is trivial (i.e. always true or always false)
        internal static bool cAtSetIsTrivial(char c, PDLSet s)
        {
            if (s is PDLIndicesOf)            
                return true;
            
            var inters = s as PDLIntersect;
            if (inters != null)
                return cAtSetIsTrivial(c, inters.set1) || cAtSetIsTrivial(c, inters.set2);

            var union = s as PDLUnion;
            if (union != null)
                return cAtSetIsTrivial(c, union.set1) || cAtSetIsTrivial(c, union.set2);

            return false;
        }

        //return true if two positions are either trivially the same or trivially different
        internal static bool SyntacticEquivIsTrivial(PDLPos p1, PDLPos p2)
        {
            if (p1 is PDLSuccessor)
            {
                var p11 = p1 as PDLSuccessor;
                return SyntacticEquivIsTrivial(p11.pos, p2);
            }
            if (p1 is PDLPredecessor)
            {
                var p11 = p1 as PDLPredecessor;
                return SyntacticEquivIsTrivial(p11.pos, p2);
            }
            if (p2 is PDLSuccessor)
            {
                var p21 = p2 as PDLSuccessor;
                return SyntacticEquivIsTrivial(p1, p21.pos);
            }
            if (p2 is PDLPredecessor)
            {
                var p21 = p2 as PDLPredecessor;
                return SyntacticEquivIsTrivial(p1, p21.pos);
            }
            return p1.CompareTo(p2)==0;
        }

        //return true if two positions are trivially different
        internal static bool SyntacticDisequivIsTrivial(PDLPos p1, PDLPos p2)
        {
            return SyntacticDisequivIsTrue(p1, p2) || SyntacticDisequivIsFalse(p1, p2);
        }

        internal static bool SyntacticDisequivIsTrue(PDLPos p1, PDLPos p2)
        {
            if (p1 is PDLPredecessor)
            {
                var p11 = p1 as PDLPredecessor;
                return SyntacticDisequivIsTrue(p11.pos, p2);
            }
            if (p2 is PDLSuccessor)
            {
                var p21 = p2 as PDLSuccessor;
                return SyntacticDisequivIsTrue(p1, p21.pos);
            }
            return p1.CompareTo(p2) == 0;
        }

        internal static bool SyntacticDisequivIsFalse(PDLPos p1, PDLPos p2)
        {
            if (p1 is PDLSuccessor)
            {
                var p11 = p1 as PDLSuccessor;
                return SyntacticDisequivIsFalse(p11.pos, p2);
            }
            if (p2 is PDLPredecessor)
            {
                var p21 = p2 as PDLPredecessor;
                return SyntacticDisequivIsFalse(p1, p21.pos);
            }
            return p1.CompareTo(p2) == 0;
        }


        private static bool IsNumericalFromLast(PDLPos phi, out int size)
        {
            if (phi is PDLLast)
            {
                size = 1;
                return true;
            }

            var v = phi as PDLPredecessor;
            if (v != null)
            {
                int s = 0;
                if (IsNumericalFromLast(v.pos, out s))
                {
                    size = s + 1;
                    return true;
                }
            }

            size = 0;
            return false;
        }

        private static bool IsNumericalFromFirst(PDLPos phi, out int size)
        {
            if (phi is PDLFirst)
            {
                size = 1;
                return true;
            }

            var v = phi as PDLSuccessor;
            if (v != null)
            {
                int s = 0;
                if (IsNumericalFromFirst(v.pos, out s))
                {
                    size = s + 1;
                    return true;
                }
            }

            size = 0;
            return false;
        }

        private static bool IsNumericalFromFirstOcc(PDLPos phi, out int size)
        {
            if (phi is PDLFirstOcc)
            {
                size = 1;
                return true;
            }

            var v1 = phi as PDLPredecessor;
            if (v1 != null)
            {
                int s = 0;
                if (IsNumericalFromFirstOcc(v1.pos, out s))
                {
                    size = s + 1;
                    return true;
                }
            }

            var v2 = phi as PDLSuccessor;
            if (v2 != null)
            {
                int s = 0;
                if (IsNumericalFromFirstOcc(v2.pos, out s))
                {
                    size = s + 1;
                    return true;
                }
            }

            size = 0;
            return false;
        }

        private static bool IsNumericalFromLastOcc(PDLPos phi, out int size)
        {
            if (phi is PDLLastOcc)
            {
                size = 1;
                return true;
            }

            var v1 = phi as PDLPredecessor;
            if (v1 != null)
            {
                int s = 0;
                if (IsNumericalFromLastOcc(v1.pos, out s))
                {
                    size = s + 1;
                    return true;
                }
            }

            var v2 = phi as PDLSuccessor;
            if (v2 != null)
            {
                int s = 0;
                if (IsNumericalFromLastOcc(v2.pos, out s))
                {
                    size = s + 1;
                    return true;
                }
            }

            size = 0;
            return false;
        }

        private static bool IsNumericalFromFirstLastOcc(PDLPos phi, out int size)
        {
            if (phi is PDLLastOcc || phi is PDLFirstOcc)
            {
                size = 1;
                return true;
            }

            var v1 = phi as PDLPredecessor;
            if (v1 != null)
            {
                int s = 0;
                if (IsNumericalFromFirstLastOcc(v1.pos, out s))
                {
                    size = s + 1;
                    return true;
                }
            }

            var v2 = phi as PDLSuccessor;
            if (v2 != null)
            {
                int s = 0;
                if (IsNumericalFromFirstLastOcc(v2.pos, out s))
                {
                    size = s + 1;
                    return true;
                }
            }

            size = 0;
            return false;
        }
        #endregion


        private static int ComparePairsPhiDoubleByDouble(Pair<PDLPred, double> x, Pair<PDLPred, double> y)
        {
            return x.Second.CompareTo(y.Second);
        }

    }
}
