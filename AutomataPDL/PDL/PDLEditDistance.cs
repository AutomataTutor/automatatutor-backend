using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Automata;

//Copyright (C) 2004 Stephen Wan

namespace AutomataPDL
{
    public static class PDLEditDistance
    {
        #region PDL edit distance
        /// <summary>
        /// Returns the minimum PDL edit distance ratio between all the PDL A1 and A2 inferred for dfa1 and dfa2
        /// in less than timeout. For every  min_ij(d(A1i,A2j)/|A1i)
        /// </summary>
        /// <param name="dfa1"></param>
        /// <param name="dfa2"></param>
        /// <param name="al"></param>
        /// <param name="solver"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public static double GetMinimalFormulaEditDistanceRatio(Automaton<BDD> dfa1, Automaton<BDD> dfa2, HashSet<char> al, CharSetSolver solver, long timeout, PDLEnumerator pdlEnumerator)
        {
            var v = GetMinimalFormulaEditDistanceTransformation(dfa1, dfa2, al, solver, timeout, pdlEnumerator);
            if(v!=null){
                var transformation = v.First;
                var scaling = 1.0;
                return transformation.totalCost / (transformation.minSizeForTreeA * scaling);
            }
            return 10;
        }

        /// <summary>
        /// Find the min formula edit distance between two dfas
        /// </summary>
        /// <param name="dfa1"></param>
        /// <param name="dfa2"></param>
        /// <param name="al"></param>
        /// <param name="solver"></param>
        /// <param name="timeout"></param>
        /// <returns>transformation of smallest cost, transf of feeedback (first one)</returns>
        public static Pair<Transformation, Transformation> GetMinimalFormulaEditDistanceTransformation(Automaton<BDD> dfa1, Automaton<BDD> dfa2, HashSet<char> al, CharSetSolver solver, long timeout, PDLEnumerator pdlEnumerator)
        {
            int maxFormulas = 30;
            //Find all the formulas describing dfa1 and dfa2
            var v = new StringBuilder();
            List<PDLPred> pdlFormulae1 = new List<PDLPred>();
            foreach (var phi in pdlEnumerator.SynthesizePDL(al, dfa1, solver, v, timeout, maxFormulas))
            {
                //Console.WriteLine(phi);
                pdlFormulae1.Add(phi);
            }

            if (pdlFormulae1.Count == 0)
                return null;

            List<PDLPred> pdlFormulae2 = new List<PDLPred>();
            //Console.WriteLine();
            foreach (var phi in pdlEnumerator.SynthesizePDL(al, dfa2, solver, v, timeout, maxFormulas))
            {
                //Console.WriteLine(phi);
                pdlFormulae2.Add(phi);
            }

            if (pdlFormulae2.Count == 0)
                return null;

            // Initialize parameters for feedback search                         
            double minSizePhi1 = 20;
            PDLPred smallestPhi1 = null;
            PDLPred closestPhi2toSmallerPhi1 = null;

            //This first enumerations find the smallest phi1 describing the solution and the closest phi2 describing the solution for feedback
            foreach (var phi1 in pdlFormulae1)
            {
                var sizePhi1 = phi1.GetFormulaSize();
                if (sizePhi1 < minSizePhi1)
                {
                    minSizePhi1 = (double)sizePhi1;
                    smallestPhi1 = phi1;
                }
            }
            // Look for the closest formula to the smallest one describing phi1
            // The formula can be at most distance 2 from the smallest phi1
            double minEd = 3;
            foreach (var phi2 in pdlFormulae2)
            {
                if (!phi2.IsComplex())
                {
                    if (minEd != 1 && ((int)Math.Abs(smallestPhi1.GetFormulaSize() - phi2.GetFormulaSize())) <= minEd)
                    {
                        var tr = GetFormulaEditDistance(smallestPhi1, phi2);
                        var fed = tr.totalCost;
                        if (fed < minEd)
                        {
                            minEd = fed;
                            closestPhi2toSmallerPhi1 = phi2;
                        }
                        //if (closestPhi2toSmallerPhi1 == null || closestPhi2toSmallerPhi1.GetFormulaSize() > phi2.GetFormulaSize())
                        //    closestPhi2toSmallerPhi1 = phi2;
                        if (minEd == 0)
                            throw new PDLException("cannot be 0");
                    }
                }
            }

            //Initialize parameters for grading search
            //This second enumerations are for grading
            minEd = 100;
            minSizePhi1 = 100;
            PDLPred phiMin1 = null;
            PDLPred phiMin2 = null;

            foreach (var phi1 in pdlFormulae1)
            {
                var sizePhi1 = phi1.GetFormulaSize();
                if (sizePhi1 < minSizePhi1)
                    minSizePhi1 = (double)sizePhi1;
                foreach (var phi2 in pdlFormulae2)
                {
                    if (minEd != 1 && ((int)Math.Abs(phi1.GetFormulaSize() - phi2.GetFormulaSize())) <= minEd)
                    {
                        var tr = GetFormulaEditDistance(phi1, phi2);
                        var fed = tr.totalCost;
                        if (fed < minEd)
                        {
                            minEd = fed;
                            phiMin1 = phi1;
                            phiMin2 = phi2;
                        }
                        if (minEd == 0)
                            throw new PDLException("cannot be 0");
                    }
                }
            }
            Transformation tgrade = GetFormulaEditDistance(phiMin1, phiMin2);
            tgrade.minSizeForTreeA = minSizePhi1;
            Transformation tfeed = null;
            if (closestPhi2toSmallerPhi1 != null)
            {
                tfeed = GetFormulaEditDistance(smallestPhi1, closestPhi2toSmallerPhi1);
                tfeed.minSizeForTreeA = minSizePhi1;
            }
            return new Pair<Transformation, Transformation>(tgrade, tfeed);
        }

        /// <summary>
        /// Returns the edit distance ration between 2 PDLpred A1,A2
        /// </summary>
        /// <param name="phi1"></param>
        /// <param name="phi2"></param>
        /// <returns>d(A1,A2)</returns>
        internal static Transformation GetFormulaEditDistance(PDLPred phi1, PDLPred phi2)
        {
            TreeEditDistance treeCorrector = new TreeEditDistance();
            TreeDefinition aTree = CreateTreeHelper.MakeTree(phi1);
            TreeDefinition bTree = CreateTreeHelper.MakeTree(phi2);
            Transformation transform = treeCorrector.getTransformation(aTree, bTree);

            return transform;
        }
        #endregion
    }

    public class TreeEditDistance
    {
        private double[,] distance = null;
        private TreeEditScript[,] distScript = null;

        //Computes tree edits distance betwee aTree and bTree
        public Transformation findDistance(TreeDefinition aTree, TreeDefinition bTree)
        {
            distance = new double[aTree.getNodeCount() + 1, bTree.getNodeCount() + 1];
            distScript = new TreeEditScript[aTree.getNodeCount() + 1, bTree.getNodeCount() + 1];

            //Preliminaries
            //1. Find left-most leaf and key roots
            Dictionary<int, int> aLeftLeaf = new Dictionary<int, int>();
            Dictionary<int, int> bLeftLeaf = new Dictionary<int, int>();
            List<int> aTreeKeyRoots = new List<int>();
            List<int> bTreeKeyRoots = new List<int>();

            findHelperTables(aTree, aLeftLeaf, aTreeKeyRoots, aTree.getRootID());

            findHelperTables(bTree, bLeftLeaf, bTreeKeyRoots, bTree.getRootID());

            var a = bTree.getRootID();
            //Comparison
            foreach (int aKeyroot in aTreeKeyRoots)
            {
                foreach (int bKeyroot in bTreeKeyRoots)
                {
                    //Re-initialise forest distance tables
                    Dictionary<int, Dictionary<int, Double>> fD =
                        new Dictionary<int, Dictionary<int, Double>>();

                    //Re-initialise forest edit script distance tables
                    Dictionary<int, Dictionary<int, TreeEditScript>> fESD =
                        new Dictionary<int, Dictionary<int, TreeEditScript>>();

                    setForestDistance(aLeftLeaf[aKeyroot], bLeftLeaf[bKeyroot], 0.0d, fD);
                    //script is automatically null                 

                    //for all descendents of aKeyroot: i
                    for (int i = aLeftLeaf[aKeyroot]; i <= aKeyroot; i++)
                    {
                        var edit = new Delete(i,aTree);

                        setForestDistance(i,
                          bLeftLeaf[bKeyroot] - 1,
                          getForestDistance(i - 1, bLeftLeaf[bKeyroot] - 1, fD) +
                          edit.getCost(),
                          fD);

                        var scriptSuffix = new TreeEditScript(getForestScript(i - 1, bLeftLeaf[bKeyroot] - 1, fESD));
                        scriptSuffix.Insert(edit);

                        setForestScript(i, bLeftLeaf[bKeyroot] - 1, scriptSuffix, fESD);
                    }

                    //for all descendents of bKeyroot: j
                    for (int j = bLeftLeaf[bKeyroot]; j <= bKeyroot; j++)
                    {
                        var edit = new Insert(j, bTree);

                        setForestDistance(aLeftLeaf[aKeyroot] - 1, j,
                          getForestDistance(aLeftLeaf[aKeyroot] - 1, j - 1, fD) +
                          edit.getCost(),
                          fD);

                        var scriptSuffix = new TreeEditScript(getForestScript(aLeftLeaf[aKeyroot] - 1, j - 1, fESD));
                        scriptSuffix.Insert(edit);

                        setForestScript(aLeftLeaf[aKeyroot] - 1, j, scriptSuffix, fESD);
                    }

                    for (int i = aLeftLeaf[aKeyroot]; i <= aKeyroot; i++)
                    {
                        for (int j = bLeftLeaf[bKeyroot]; j <= bKeyroot; j++)
                        {
                            TreeEditScript tempScript = null;

                            EditOperation delEdit = new Delete(i, aTree); ;
                            EditOperation insEdit = new Insert(j, bTree);
                            double min;
                            double delCost = getForestDistance(i - 1, j, fD) + delEdit.getCost();
                            double insCost = getForestDistance(i, j - 1, fD) + insEdit.getCost();

                            //This min compares del vs ins
                            if (delCost <= insCost)
                            {
                                //Option 1: Delete node from aTree
                                tempScript = new TreeEditScript(getForestScript(i - 1, j, fESD));
                                tempScript.Insert(delEdit);
                                min = delCost;
                            }
                            else
                            {
                                //Option 2: Insert node into bTree
                                tempScript = new TreeEditScript(getForestScript(i, j - 1, fESD));
                                tempScript.Insert(insEdit);
                                min = insCost;
                            }

                            if (aLeftLeaf[i] == aLeftLeaf[aKeyroot] && bLeftLeaf[j] == bLeftLeaf[bKeyroot])
                            {
                                var renEdit = new Rename(i, j, aTree, bTree);
                                var dist = getForestDistance(i - 1, j - 1, fD) + renEdit.getCost();

                                distance[i, j] = Math.Min(min, dist);

                                if (min <= dist)
                                {
                                    tempScript = new TreeEditScript(tempScript);
                                    distScript[i, j] = tempScript;
                                }
                                else
                                {
                                    tempScript = new TreeEditScript(getForestScript(i - 1, j - 1, fESD));
                                    tempScript.Insert(renEdit);
                                    distScript[i, j] = tempScript;
                                }
                                setForestDistance(i, j, distance[i, j], fD);
                                setForestScript(i, j, new TreeEditScript(distScript[i, j]), fESD);

                            }
                            else
                            {
                                var value = getForestDistance(aLeftLeaf[i] - 1, bLeftLeaf[j] - 1, fD) + distance[i, j];
                                setForestDistance(i, j, Math.Min(min, value), fD);

                                if (min <= value)
                                    setForestScript(i, j, new TreeEditScript(tempScript), fESD);
                                else
                                {
                                    var tempList = getForestScript(aLeftLeaf[i] - 1, bLeftLeaf[j] - 1, fESD).script;
                                    tempList = distScript[i, j].script.Concat(tempList).ToList();

                                    setForestScript(i, j, new TreeEditScript(tempList), fESD);
                                }

                                setForestDistance(i, j, Math.Min(min, value), fD);
                                
                            }
                        }
                    }
                }
            }

            Transformation transform = new Transformation();
            transform.totalCost = distance[aTree.getNodeCount(), bTree.getNodeCount()];
            transform.editScriptAtoB = distScript[aTree.getNodeCount(), bTree.getNodeCount()];
            transform.pdlA = aTree.pdl;
            transform.pdlB = bTree.pdl;
            transform.pdlMappingTreeB = bTree.pdlNodeMapping;
            transform.pdlMappingTreeA = aTree.pdlNodeMapping;
            return transform;
        }

        public Transformation getTransformation(TreeDefinition aTree, TreeDefinition bTree)
        {
            Transformation transform = new Transformation();            
            Transformation t1 = findDistance(aTree, bTree);
            transform.totalCost = distance[aTree.getNodeCount(), bTree.getNodeCount()];
            Transformation t2 = findDistance(bTree, aTree);
            
            transform.editScriptAtoB = t1.editScriptAtoB;
            transform.editScriptBtoA = t2.editScriptAtoB;
            transform.pdlA = t1.pdlA;
            transform.pdlB = t1.pdlB;
            transform.pdlMappingTreeB = t1.pdlMappingTreeB;
            transform.pdlMappingTreeA = t1.pdlMappingTreeA;
            return transform;
        }



        /** The initiating call should be to the root node of the tree.
         * It fills in an nxn (hash) table of the leftmost leaf for a
         * given node.  It also compiles an array of key roots. The
         * int values IDs must come from the post-ordering of the
         * nodes in the tree.
         */
        private void findHelperTables(TreeDefinition someTree, Dictionary<int, int> leftmostLeaves, List<int> keyroots, int aNodeID)
        {
            findHelperTablesRecurse(someTree, leftmostLeaves, keyroots, aNodeID);

            //add root to keyroots
            keyroots.Add(aNodeID);

            //add boundary nodes
            leftmostLeaves[0] = 0;
        }

        private void findHelperTablesRecurse(TreeDefinition someTree, Dictionary<int, int> leftmostLeaves, List<int> keyroots, int aNodeID)
        {

            //If this is a leaf, then it is the leftmost leaf
            if (someTree.isLeaf(aNodeID))
            {
                leftmostLeaves[aNodeID] = aNodeID;
            }
            else
            {
                bool seenLeftmost = false;
                foreach (int child in someTree.getChildrenIDs(aNodeID))
                {
                    findHelperTablesRecurse(someTree, leftmostLeaves, keyroots, child);
                    if (!seenLeftmost)
                    {
                        leftmostLeaves[aNodeID] = leftmostLeaves[child];
                        seenLeftmost = true;
                    }
                    else
                        keyroots.Add(child);
                }
            }
        }

        /** Returns a String for trace writes */
        private void seeFD(int a, int b,
                   Dictionary<int, Dictionary<int, Double>>
                   forestDistance)
        {

            Console.WriteLine("[" + a + "],[" + b + "]: " + getForestDistance(a, b, forestDistance));
        }

        /** Returns a String for trace writes */
        private void seeFD(Dictionary<int, Dictionary<int, Double>>
                   forestDistance)
        {

            Console.WriteLine("Forest Distance");
            //Return result
            foreach (int i in new HashSet<int>(forestDistance.Keys))
            {
                Console.Write(i + ": ");
                foreach (int j in new HashSet<int>(forestDistance[i].Keys))
                    Console.Write(forestDistance[i][j] + "(" + j + ")  ");
                Console.WriteLine("");
            }
        }

        /** This returns the value in the cell of the ForestDistance table
         */
        private double getForestDistance(int a, int b,
                   Dictionary<int, Dictionary<int, Double>>
                   forestDistance)
        {

            Dictionary<int, Double> rows = null;
            if (!forestDistance.ContainsKey(a))
                forestDistance[a] = new Dictionary<int, Double>();           

            rows = forestDistance[a];
            if (!rows.ContainsKey(b))
                rows[b] = 0.0;
            
            return rows[b];
        }

        /** This returns the value in the cell of the ForestDistance table
         */
        private TreeEditScript getForestScript(int a, int b,
                   Dictionary<int, Dictionary<int, TreeEditScript>>
                   forestES)
        {

            Dictionary<int, TreeEditScript> rows = null;
            if (!forestES.ContainsKey(a))
                forestES[a] = new Dictionary<int, TreeEditScript>();

            rows = forestES[a];
            if (!rows.ContainsKey(b))
                rows[b] = new TreeEditScript();

            return rows[b];
        }


        /** This sets the value in the cell of the ForestDistance table
         */
        private void setForestDistance(int a, int b,
                   double aValue,
                   Dictionary<int, Dictionary<int, Double>>
                   forestDistance)
        {

            Dictionary<int, Double> rows = null;
            if (!forestDistance.ContainsKey(a))
                forestDistance[a] = new Dictionary<int, Double>();

            rows = forestDistance[a];
            rows[b] = aValue;
        }

        /** This sets the value in the cell of the ForestDistance table
         */
        private void setForestScript(int a, int b,
                   TreeEditScript script,
                   Dictionary<int, Dictionary<int, TreeEditScript>>
                   forestES)
        {

            Dictionary<int, TreeEditScript> rows = null;
            if (!forestES.ContainsKey(a))
                forestES[a] = new Dictionary<int, TreeEditScript>();

            rows = forestES[a];
            rows[b] = script;
        }
    }

    public class Transformation
    {
        internal double totalCost;
        public TreeEditScript editScriptAtoB;
        public TreeEditScript editScriptBtoA;
        internal PDLPred pdlA;
        internal PDLPred pdlB;
        internal Dictionary<string, Pair<PDL, string>> pdlMappingTreeA;
        internal Dictionary<string, Pair<PDL, string>> pdlMappingTreeB;
        internal double minSizeForTreeA;

        public Transformation()
        {
            totalCost = 0;
            editScriptAtoB = new TreeEditScript();
            editScriptBtoA = new TreeEditScript();
            pdlA = null;
            pdlB = null;
            pdlMappingTreeA = new Dictionary<string,Pair<PDL,string>>();
            pdlMappingTreeB = new Dictionary<string, Pair<PDL, string>>();
            minSizeForTreeA = 0;
        }

        public void ToString(StringBuilder sb)
        {
            sb.AppendLine(editScriptAtoB.ToString());
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            this.ToString(sb);
            return sb.ToString();
        }

        public string ToHTMLColoredStringAtoB(string delColor, string renColor)
        {
            StringBuilder sb = new StringBuilder();
            HashSet<PDL> renamed = new HashSet<PDL>();
            HashSet<PDL> deleted = new HashSet<PDL>();
            foreach (var edit in this.editScriptAtoB.script)
                if (edit.getCost() > 0)
                {
                    if (edit is Delete)
                    {
                        deleted.Add((edit as Delete).pdlA.First);
                    }
                    else
                        if (edit is Rename)
                        {
                            renamed.Add((edit as Rename).pdlA.First);
                        }
                }
                
            

            ToHTMLColoredEnglishStringExt(renamed, deleted, pdlA, delColor,renColor, editScriptAtoB, sb, "s");
            return sb.ToString();
        }

        public string ToHTMLColoredStringBtoA(string delColor, string renColor)
        {
            StringBuilder sb = new StringBuilder();
            HashSet<PDL> renamed = new HashSet<PDL>();
            HashSet<PDL> deleted = new HashSet<PDL>();
            foreach (var edit in this.editScriptBtoA.script)
                if (edit.getCost() > 0)
                {
                    if (edit is Delete)
                    {
                        deleted.Add((edit as Delete).pdlA.First);
                    }
                    else
                        if (edit is Rename)
                        {
                            renamed.Add((edit as Rename).pdlA.First);
                        }
                }

            ToHTMLColoredEnglishStringExt(renamed, deleted, pdlB, delColor, renColor, editScriptBtoA, sb, "s");
            return sb.ToString();
        }

        public string ToEnglishString(PDL term)
        {
            StringBuilder sb = new StringBuilder();
            ToEnglishString(term,sb);
            return sb.ToString();
        }

        public void ToEnglishString(PDL term, StringBuilder sb)
        {
            ToHTMLColoredEnglishStringExt(new HashSet<PDL>(), new HashSet<PDL>(), term, "black", "black", editScriptAtoB, sb,"s");
        }

        private void ToHTMLColoredEnglishStringExt(HashSet<PDL> renamed, HashSet<PDL> deleted, PDL term, string delColor, string renColor, TreeEditScript editScript, StringBuilder sb, string varName)
        {
            Pattern.Match(term).
                Case<PDLFalse>(() => TermCase(renamed, deleted, term, delColor, renColor, "no string", sb)).
                Case<PDLTrue>(() => TermCase(renamed, deleted, term, delColor, renColor, "every input string", sb)).
                Case<PDLEmptyString>(() => TermCase(renamed, deleted, term, delColor, renColor, "the empty string", sb)).
                Default(() => {
                     sb.Append("{ <i>s</i> | ");
                    ToHTMLColoredEnglishStringInt(renamed, deleted, term, delColor, renColor, editScript, sb, varName);
                     sb.Append(" }");
                });           
        }

        private void ToHTMLColoredEnglishStringInt(HashSet<PDL> renamed, HashSet<PDL> deleted, PDL term, string delColor, string renColor, TreeEditScript editScript, StringBuilder sb, string varName)
        {
            bool negate = false;
            var notterm = term as PDLNot;
            if (notterm != null)
            {
                term = notterm.phi;
                negate = true;
            }

            Pattern.Match(term).
                Case<PDLAllPos>(() => TermCase(renamed, deleted, term, delColor, renColor, "the set of all positions", sb)).

                Case<PDLAtPos, char, PDLPos>((label, pos) =>
                {
                    ToHTMLColoredEnglishStringInt(renamed, deleted, pos, delColor, renColor, editScript, sb, varName);
                    if (negate)
                        TermCase(renamed, deleted, term, "", delColor, renColor, " doesn't have label ", editScript, sb);
                    else
                        TermCase(renamed, deleted, term, "", delColor, renColor, " has label ", editScript, sb);
                    TermCase(renamed, deleted, term, "label", delColor, renColor, label.ToString(), editScript, sb);
                }).

                Case<PDLAtSet, char, PDLSet>((label, set) =>
                {
                    Pattern.Match(set).
                        Case<PDLAllPos>(() =>
                        {
                            if (negate)
                                TermCase(renamed, deleted, set, delColor, renColor, string.Format("not every symbol in <i>{0}</i>", varName), sb);
                            else
                                TermCase(renamed, deleted, set, delColor, renColor, string.Format("every symbol in <i>{0}</i>", varName), sb);
                            TermCase(renamed, deleted, term, "", delColor, renColor, " is a '", editScript, sb);
                            TermCase(renamed, deleted, term, "label", delColor, renColor, label.ToString(), editScript, sb);
                            TermCase(renamed, deleted, term, "", delColor, renColor, string.Format("'"), editScript, sb);
                        }).
                        Case<PDLSetCmpPos, PDLPos, PDLComparisonOperator>((pos, op) =>
                        {
                            if (negate)
                                TermCase(renamed, deleted, set, "", delColor, renColor, "not every symbol ", editScript, sb);
                            else
                                TermCase(renamed, deleted, set, "", delColor, renColor, "every symbol ", editScript, sb);
                            switch (op)
                            {
                                case PDLComparisonOperator.Ge: TermCase(renamed, deleted, set, delColor, renColor, "after ", sb); break;
                                case PDLComparisonOperator.Geq: TermCase(renamed, deleted, set, delColor, renColor, "after and including ", sb); break;
                                case PDLComparisonOperator.Le: TermCase(renamed, deleted, set, delColor, renColor, "before ", sb); break;
                                case PDLComparisonOperator.Leq: TermCase(renamed, deleted, set, delColor, renColor, "before and including ", sb); break;
                                default: throw new PDLException("undefined operator");
                            }
                            Pattern.Match(pos).
                                Case<PDLFirst>(() =>
                                {
                                    TermCase(renamed, deleted, pos, delColor, renColor, "the first one", sb);
                                }).
                                Case<PDLLast>(() =>
                                {
                                    TermCase(renamed, deleted, pos, delColor, renColor, "the last one", sb);
                                }).
                                Default(() =>
                                {
                                    ToHTMLColoredEnglishStringInt(renamed, deleted, pos, delColor, renColor, editScript, sb, varName);
                                });
                            TermCase(renamed, deleted, term, "", delColor, renColor, " has label ", editScript, sb);
                            TermCase(renamed, deleted, term, delColor, renColor, string.Format("'"), sb);
                            TermCase(renamed, deleted, term, "label", delColor, renColor, label.ToString(), editScript, sb);
                            TermCase(renamed, deleted, term, delColor, renColor, string.Format("'"), sb);
                        }).
                        Case<PDLPredSet, string, PDLPred>((var, phi) =>
                            {
                                Pattern.Match(phi).
                                    Case<PDLModSetEq, PDLSet, int, int>((set1, m, n) =>
                                    {
                                        var cast = set1 as PDLAllPosUpto;
                                        if (m == 2 && cast != null && cast.pos is PDLPosVar)
                                        {
                                            if (negate)
                                                TermCase(renamed, deleted, set, "", delColor, renColor, "not every ", editScript, sb);
                                            else
                                                TermCase(renamed, deleted, set, "", delColor, renColor, "every ", editScript, sb);
                                            TermCase(renamed, deleted, phi, "n", delColor, renColor, (n == 1 ? "odd" : "even"), editScript, sb);
                                            TermCase(renamed, deleted, set, "", delColor, renColor, " position", editScript, sb);
                                            TermCase(renamed, deleted, term, "", delColor, renColor, " has label '", editScript, sb);
                                            TermCase(renamed, deleted, term, "label", delColor, renColor, label.ToString(), editScript, sb);
                                            TermCase(renamed, deleted, term, delColor, renColor, string.Format("'"), sb);
                                        }
                                        else
                                        {
                                            if (negate)
                                                TermCase(renamed, deleted, set, "", delColor, renColor, "not every position " + var + " such that ", editScript, sb);
                                            else
                                                TermCase(renamed, deleted, set, "", delColor, renColor, "every position " + var + " such that ", editScript, sb);
                                            ToHTMLColoredEnglishStringInt(renamed, deleted, phi, delColor, renColor, editScript, sb, varName);
                                            TermCase(renamed, deleted, term, "", delColor, renColor, " has label '", editScript, sb);
                                            TermCase(renamed, deleted, term, "label", delColor, renColor, label.ToString(), editScript, sb);
                                            TermCase(renamed, deleted, term, delColor, renColor, string.Format("'"), sb);
                                        }
                                    }).
                                    Default(() =>
                                    {
                                        if (negate)
                                            TermCase(renamed, deleted, set, "", delColor, renColor, "not every position " + var + " such that ", editScript, sb);
                                        else
                                            TermCase(renamed, deleted, set, "", delColor, renColor, "every position " + var + " such that ", editScript, sb);
                                        ToHTMLColoredEnglishStringInt(renamed, deleted, phi, delColor, renColor, editScript, sb, varName);
                                        TermCase(renamed, deleted, term, "", delColor, renColor, " has label '", editScript, sb);
                                        TermCase(renamed, deleted, term, "label", delColor, renColor, label.ToString(), editScript, sb);
                                        TermCase(renamed, deleted, term, delColor, renColor, string.Format("'"), sb);
                                    });
                            }).
                        Default(() =>
                        {
                            if (negate)
                                TermCase(renamed, deleted, term, "", delColor, renColor, "not every position in ", editScript, sb);
                            else
                                TermCase(renamed, deleted, term, "", delColor, renColor, "every position in ", editScript, sb);
                            ToHTMLColoredEnglishStringInt(renamed, deleted, set, delColor, renColor, editScript, sb, varName);
                            TermCase(renamed, deleted, term, "", delColor, renColor, " has label '", editScript, sb);
                            TermCase(renamed, deleted, term, "label", delColor, renColor, label.ToString(), editScript, sb);
                            TermCase(renamed, deleted, term, delColor, renColor, string.Format("'"), sb);
                        });
                }).


                Case<PDLBelongs, PDLPos, PDLSet>((pos, set) =>
                {
                    ToHTMLColoredEnglishStringInt(renamed, deleted, pos, delColor, renColor, editScript, sb, varName);
                    if (negate)
                        TermCase(renamed, deleted, term, delColor, renColor, " doesn't belong to ", sb);
                    else
                        TermCase(renamed, deleted, term, delColor, renColor, " belongs to ", sb);
                    ToHTMLColoredEnglishStringInt(renamed, deleted, set, delColor, renColor, editScript, sb, varName);
                }).

                Case<PDLBinaryFormula, PDLPred, PDLPred, PDLLogicalOperator>((phi1, phi2, op) =>
                {
                    if (negate)
                        TermCase(renamed, deleted, notterm, delColor, renColor, "it is not the case that ", sb);

                    switch (op)
                    {
                        case PDLLogicalOperator.And:
                            {
                                ToHTMLColoredEnglishStringInt(renamed, deleted, phi1, delColor, renColor, editScript, sb, varName);
                                sb.Append(", ");
                                TermCase(renamed, deleted, term, delColor, renColor, "and ", sb);
                                break;
                            }
                        case PDLLogicalOperator.If:
                            {
                                TermCase(renamed, deleted, term, delColor, renColor, "if ", sb);
                                ToHTMLColoredEnglishStringInt(renamed, deleted, phi1, delColor, renColor, editScript, sb, varName);
                                sb.Append(", ");
                                TermCase(renamed, deleted, term, delColor, renColor, "then ", sb);
                                break;
                            }
                        case PDLLogicalOperator.Iff:
                            {
                                ToHTMLColoredEnglishStringInt(renamed, deleted, phi1, delColor, renColor, editScript, sb, varName);
                                sb.Append(", ");
                                TermCase(renamed, deleted, term, delColor, renColor, "if and only if ", sb);
                                break;
                            }
                        case PDLLogicalOperator.Or:
                            {
                                ToHTMLColoredEnglishStringInt(renamed, deleted, phi1, delColor, renColor, editScript, sb, varName);
                                sb.Append(", ");
                                TermCase(renamed, deleted, term, delColor, renColor, "or ", sb);
                                break;
                            }
                        default: throw new PDLException("undefined operator");
                    }

                    ToHTMLColoredEnglishStringInt(renamed, deleted, phi2, delColor, renColor, editScript, sb, varName);
                }).

                Case<PDLBinaryPosFormula, PDLPos, PDLPos, PDLPosComparisonOperator>((pos1, pos2, op) =>
                {
                    ToHTMLColoredEnglishStringInt(renamed, deleted, pos1, delColor, renColor, editScript, sb, varName);
                    switch (op)
                    {
                        case PDLPosComparisonOperator.Eq:
                            {
                                if (negate)
                                    TermCase(renamed, deleted, term, delColor, renColor, " isn't the same as ", sb); 
                                else
                                    TermCase(renamed, deleted, term, delColor, renColor, " is the same as ", sb); 
                                break;
                            }

                        case PDLPosComparisonOperator.Ge:
                            {
                                if (negate)
                                    TermCase(renamed, deleted, term, delColor, renColor, " isn't after ", sb); 
                                else
                                    TermCase(renamed, deleted, term, delColor, renColor, " is after ", sb); 
                                break;                                
                            }

                        case PDLPosComparisonOperator.Geq:
                            {
                                if(negate)
                                    TermCase(renamed, deleted, term, delColor, renColor, " isn't after or the same as ", sb); 
                                else
                                    TermCase(renamed, deleted, term, delColor, renColor, " is after or the same as ", sb); 
                                break;
                            }

                        case PDLPosComparisonOperator.Le:
                            {
                                if (negate)
                                    TermCase(renamed, deleted, term, delColor, renColor, " isn't before ", sb); 
                                else
                                    TermCase(renamed, deleted, term, delColor, renColor, " is before ", sb); 
                                break;                                
                            }

                        case PDLPosComparisonOperator.Leq:
                            {
                                if (negate)
                                    TermCase(renamed, deleted, term, delColor, renColor, " isn't before or the same as ", sb);
                                else
                                    TermCase(renamed, deleted, term, delColor, renColor, " is before or is the same as ", sb);
                                break; 
                            }

                        case PDLPosComparisonOperator.Pred:
                            {
                                if (negate)
                                    TermCase(renamed, deleted, term, delColor, renColor, " isn't right before ", sb); 
                                else
                                    TermCase(renamed, deleted, term, delColor, renColor, " is right before ", sb); 
                                break;
                            }

                        case PDLPosComparisonOperator.Succ:
                            {
                                if (negate)
                                    TermCase(renamed, deleted, term, delColor, renColor, " isn't right after ", sb); 
                                else
                                    TermCase(renamed, deleted, term, delColor, renColor, " is right after ", sb);
                                break;
                            }

                        default: throw new PDLException("Undefined operator");
                    }
                    ToHTMLColoredEnglishStringInt(renamed, deleted, pos2, delColor, renColor, editScript, sb, varName);
                }).

                Case<PDLEmptyString>(() =>
                {
                    if(negate)
                        TermCase(renamed, deleted, term, delColor, renColor, string.Format("<i>s</i> isn't the empty string", varName), sb);
                    else
                        TermCase(renamed, deleted, term, delColor, renColor, string.Format("<i>s</i> is the empty string", varName), sb);
                }).

                Case<PDLFalse>(() => TermCase(renamed, deleted, term, delColor, renColor, "false", sb)).

                Case<PDLIndicesOf, string>((str) =>
                {
                    TermCase(renamed, deleted, term, "", delColor, renColor, "the set of positions where a substring '", editScript, sb);
                    TermCase(renamed, deleted, term, "str", delColor, renColor, str, editScript, sb);
                    TermCase(renamed, deleted, term, "", delColor, renColor, "' starts", editScript, sb);
                }).

                Case<PDLQuantifiedFormula, PDLPred, String, PDLQuantifier>((phi, var, q) =>
                {
                    switch (q)
                    {
                        case PDLQuantifier.ExistsFO:
                            {
                                Pattern.Match(phi).
                                    Case<PDLAtPos, char, PDLPos>((label, pos) =>
                                    {
                                        if(negate)
                                            TermCase(renamed, deleted, term, delColor, renColor, string.Format("<i>{0}</i> doesn't contain at least one '", varName), sb);
                                        else
                                            TermCase(renamed, deleted, term, delColor, renColor, string.Format("<i>{0}</i> contains at least one '", varName), sb);
                                        TermCase(renamed, deleted, phi, "label", delColor, renColor, label.ToString(), editScript, sb);
                                        TermCase(renamed, deleted, term, delColor, renColor, string.Format("'"), sb);
                                    }).
                                    Default(() =>
                                    {
                                        if(negate)
                                            TermCase(renamed, deleted, term, delColor, renColor, string.Format("there doesn't exist a position <i>{0}</i> in <i>{1}</i>, ", var, varName), sb);
                                        else
                                            TermCase(renamed, deleted, term, delColor, renColor, string.Format("there exists a position <i>{0}</i> in <i>{1}</i>, ", var, varName), sb);
                                        ToHTMLColoredEnglishStringInt(renamed, deleted, phi, delColor, renColor, editScript, sb, varName);
                                    });
                                break;
                            }
                        case PDLQuantifier.ExistsSO:
                            {
                                if(negate)
                                    TermCase(renamed, deleted, term, delColor, renColor, string.Format("there doesn't exist a set of positions <i>{0}</i> in <i>{1}</i>, ", var, varName), sb);
                                else
                                    TermCase(renamed, deleted, term, delColor, renColor, string.Format("there exists a set of positions <i>{0}</i> in <i>{1}</i>, ", var, varName), sb);
                                ToHTMLColoredEnglishStringInt(renamed, deleted, phi, delColor, renColor, editScript, sb, varName);
                                break;
                            }
                        case PDLQuantifier.ForallFO:
                            {
                                Pattern.Match(phi).
                                    Case<PDLAtPos, char, PDLPos>((label, pos) =>
                                    {
                                        if(negate)
                                            TermCase(renamed, deleted, term, delColor, renColor, string.Format("not every symbol in <i>{0}</i> is a '", varName), sb);
                                        else
                                            TermCase(renamed, deleted, term, delColor, renColor, string.Format("every symbol in <i>{0}</i> is a '", varName), sb);
                                        TermCase(renamed, deleted, phi, "label", delColor, renColor, label.ToString(), editScript, sb);
                                        TermCase(renamed, deleted, term, delColor, renColor, string.Format("'"), sb);
                                    }).
                                    Default(() =>
                                    {
                                        if(negate)
                                            TermCase(renamed, deleted, term, delColor, renColor, string.Format("not every position <i>{0}</i> in <i>{1}</i>, ", var, varName), sb);
                                        else
                                            TermCase(renamed, deleted, term, delColor, renColor, string.Format("for every position <i>{0}</i> in <i>{1}</i>, ", var, varName), sb);
                                        ToHTMLColoredEnglishStringInt(renamed, deleted, phi, delColor, renColor, editScript, sb, varName);
                                    });
                                break;
                            }
                        case PDLQuantifier.ForallSO:
                            {
                                if(negate)
                                    TermCase(renamed, deleted, term, delColor, renColor, string.Format("not every set of positions <i>{0}</i> in <i>{1}</i>, ", var, varName), sb);
                                else
                                    TermCase(renamed, deleted, term, delColor, renColor, string.Format("for every set of positions <i>{0}</i> in <i>{1}</i>, ", var, varName), sb);
                                ToHTMLColoredEnglishStringInt(renamed, deleted, phi, delColor, renColor, editScript, sb, varName);
                                break;
                            }
                        default: throw new PDLException("Quantifier undefined");
                    }
                }).

                Case<PDLSetCardinality, PDLSet, int, PDLComparisonOperator>((set, n, op) =>
                {
                    Pattern.Match(set).
                        Case<PDLAllPos>(() =>
                        {
                            if (negate)
                                TermCase(renamed, deleted, term, delColor, renColor, string.Format("<i>{0}</i> doesn't have length", varName), sb);
                            else
                                TermCase(renamed, deleted, term, delColor, renColor, string.Format("<i>{0}</i> has length", varName), sb);
                            switch (op)
                            {
                                case PDLComparisonOperator.Eq: TermCase(renamed, deleted, term, "", delColor, renColor, string.Format(" exactly "), editScript, sb); break;
                                case PDLComparisonOperator.Ge: TermCase(renamed, deleted, term, "", delColor, renColor, string.Format(" greater than "), editScript, sb); break;
                                case PDLComparisonOperator.Geq: TermCase(renamed, deleted, term, "", delColor, renColor, string.Format(" greater or equal than "), editScript, sb); break;
                                case PDLComparisonOperator.Le: TermCase(renamed, deleted, term, "", delColor, renColor, string.Format(" samller than "), editScript, sb); break;
                                case PDLComparisonOperator.Leq: TermCase(renamed, deleted, term, "", delColor, renColor, string.Format(" smaller or equal than "), editScript, sb); break;
                                default: throw new PDLException("Undefined operator");
                            }
                            TermCase(renamed, deleted, term, "n", delColor, renColor, n.ToString(), editScript, sb);
                        }).
                        Case<PDLIndicesOf, string>((str) =>
                        {
                            if (str.Length == 1)
                            {
                                if (negate)
                                    TermCase(renamed, deleted, set, "", delColor, renColor, string.Format("<i>{0}</i> doesn't contain", varName), editScript, sb);
                                else
                                    TermCase(renamed, deleted, set, "", delColor, renColor, string.Format("<i>{0}</i> contains", varName), editScript, sb);

                                switch (op)
                                {
                                    case PDLComparisonOperator.Eq: TermCase(renamed, deleted, term, "", delColor, renColor, string.Format(" exactly "), editScript, sb); break;
                                    case PDLComparisonOperator.Ge: TermCase(renamed, deleted, term, "", delColor, renColor, string.Format(" more than "), editScript, sb); break;
                                    case PDLComparisonOperator.Geq: TermCase(renamed, deleted, term, "", delColor, renColor, string.Format(" at least "), editScript, sb); break;
                                    case PDLComparisonOperator.Le: TermCase(renamed, deleted, term, "", delColor, renColor, string.Format(" less than "), editScript, sb); break;
                                    case PDLComparisonOperator.Leq: TermCase(renamed, deleted, term, "", delColor, renColor, string.Format(" at most "), editScript, sb); break;
                                    default: throw new PDLException("Undefined operator");
                                }
                                if (n == 1)
                                    TermCase(renamed, deleted, term, "n", delColor, renColor, "one ", editScript, sb);
                                else
                                    if (n == 2)
                                        TermCase(renamed, deleted, term, "n", delColor, renColor, "two ", editScript, sb);
                                    else
                                        if (n == 3)
                                            TermCase(renamed, deleted, term, "n", delColor, renColor, "three ", editScript, sb);
                                        else
                                            TermCase(renamed, deleted, term, "n", delColor, renColor, n.ToString()+" ", editScript, sb);                                
                                TermCase(renamed, deleted, set, "str", delColor, renColor, str, editScript, sb);
                                TermCase(renamed, deleted, term, "", delColor, renColor, " ", editScript, sb);
                                if (n > 1 || n == 0)
                                    TermCase(renamed, deleted, set, "", delColor, renColor, "'s", editScript, sb);
                            }
                            else
                            {
                                TermCase(renamed, deleted, set, "str", delColor, renColor, str, editScript, sb);
                                if (negate)
                                    TermCase(renamed, deleted, set, "", delColor, renColor, string.Format(" doesn't appear in <i>{0}</i>", varName), editScript, sb);
                                else
                                    TermCase(renamed, deleted, set, "", delColor, renColor, string.Format(" appears in <i>{0}</i> ", varName), editScript, sb);

                                switch (op)
                                {
                                    case PDLComparisonOperator.Eq: TermCase(renamed, deleted, term, "", delColor, renColor, string.Format(" exactly "), editScript, sb); break;
                                    case PDLComparisonOperator.Ge: TermCase(renamed, deleted, term, "", delColor, renColor, string.Format(" more than "), editScript, sb); break;
                                    case PDLComparisonOperator.Geq: TermCase(renamed, deleted, term, "", delColor, renColor, string.Format(" at least "), editScript, sb); break;
                                    case PDLComparisonOperator.Le: TermCase(renamed, deleted, term, "", delColor, renColor, string.Format(" less than "), editScript, sb); break;
                                    case PDLComparisonOperator.Leq: TermCase(renamed, deleted, term, "", delColor, renColor, string.Format(" at most "), editScript, sb); break;
                                    default: throw new PDLException("Undefined operator");
                                }
                                if (n == 1)
                                    TermCase(renamed, deleted, term, "n", delColor, renColor, "once", editScript, sb);
                                else
                                    if (n == 2)
                                        TermCase(renamed, deleted, term, "n", delColor, renColor, "twice", editScript, sb);
                                    else
                                        TermCase(renamed, deleted, term, "n", delColor, renColor, n.ToString() +" times", editScript, sb);                               
                            }
                        }).
                        Case<PDLUnion, PDLSet, PDLSet>((set1, set2) =>
                        {
                            if (set1 is PDLIndicesOf && set2 is PDLIndicesOf)
                            {
                                var phi1cast = set1 as PDLIndicesOf;
                                var phi2cast = set2 as PDLIndicesOf;
                                TermCase(renamed, deleted, set, "", delColor, renColor, string.Format("' the total number of occurences of "), editScript, sb);
                                TermCase(renamed, deleted, set, "", delColor, renColor, string.Format("'"), editScript, sb);
                                TermCase(renamed, deleted, set1, "str", delColor, renColor, phi1cast.str, editScript, sb);
                                TermCase(renamed, deleted, set, "", delColor, renColor, string.Format("' and '"), editScript, sb);
                                TermCase(renamed, deleted, set2, "str", delColor, renColor, phi2cast.str, editScript, sb);
                                TermCase(renamed, deleted, set, "", delColor, renColor, string.Format("'"), editScript, sb);
                                if(negate)
                                    TermCase(renamed, deleted, set, "", delColor, renColor, string.Format("' in <i>{0}</i> isn't", varName), editScript, sb);
                                else
                                    TermCase(renamed, deleted, set, "", delColor, renColor, string.Format("' in <i>{0}</i> is", varName), editScript, sb);
                                switch (op)
                                {
                                    case PDLComparisonOperator.Eq: TermCase(renamed, deleted, term, "", delColor, renColor, string.Format(" exactly "), editScript, sb); break;
                                    case PDLComparisonOperator.Ge: TermCase(renamed, deleted, term, "", delColor, renColor, string.Format(" greater than "), editScript, sb); break;
                                    case PDLComparisonOperator.Geq: TermCase(renamed, deleted, term, "", delColor, renColor, string.Format(" at least "), editScript, sb); break;
                                    case PDLComparisonOperator.Le: TermCase(renamed, deleted, term, "", delColor, renColor, string.Format(" less than "), editScript, sb); break;
                                    case PDLComparisonOperator.Leq: TermCase(renamed, deleted, term, "", delColor, renColor, string.Format(" at most "), editScript, sb); break;
                                    default: throw new PDLException("Undefined operator");
                                }
                                TermCase(renamed, deleted, term, "n", delColor, renColor, n.ToString(), editScript, sb);
                            }
                            else
                            {
                                ToHTMLColoredEnglishStringInt(renamed, deleted, set, delColor, renColor, editScript, sb, varName);
                                if(negate)
                                    TermCase(renamed, deleted, term, "", delColor, renColor, string.Format(" doesn't have"), editScript, sb);
                                else
                                    TermCase(renamed, deleted, term, "", delColor, renColor, string.Format(" has"), editScript, sb);
                               switch (op)
                                    {
                                        case PDLComparisonOperator.Eq: TermCase(renamed, deleted, term, "", delColor, renColor, string.Format(" exactly "), editScript, sb); break;
                                        case PDLComparisonOperator.Ge: TermCase(renamed, deleted, term, "", delColor, renColor, string.Format(" size greater than "), editScript, sb); break;
                                        case PDLComparisonOperator.Geq: TermCase(renamed, deleted, term, "", delColor, renColor, string.Format(" size greater or equal than "), editScript, sb); break;
                                        case PDLComparisonOperator.Le: TermCase(renamed, deleted, term, "", delColor, renColor, string.Format(" size smaller than "), editScript, sb); break;
                                        case PDLComparisonOperator.Leq: TermCase(renamed, deleted, term, "", delColor, renColor, string.Format(" size smaller or equal than "), editScript, sb); break;
                                        default: throw new PDLException("Undefined operator");
                                    }
                                TermCase(renamed, deleted, term, "n", delColor, renColor, n.ToString(), editScript, sb);
                                TermCase(renamed, deleted, term, "", delColor, renColor, " elements ", editScript, sb);
                            }
                        }).
                        Default(() =>
                        {
                            ToHTMLColoredEnglishStringInt(renamed, deleted, set, delColor, renColor, editScript, sb, varName);
                            if (negate)
                                TermCase(renamed, deleted, term, "", delColor, renColor, string.Format(" doesn't have"), editScript, sb);
                            else
                                TermCase(renamed, deleted, term, "", delColor, renColor, string.Format(" has"), editScript, sb);
                            switch (op)
                            {
                                case PDLComparisonOperator.Eq: TermCase(renamed, deleted, term, "", delColor, renColor, string.Format(" exactly "), editScript, sb); break;
                                case PDLComparisonOperator.Ge: TermCase(renamed, deleted, term, "", delColor, renColor, string.Format(" size greater than "), editScript, sb); break;
                                case PDLComparisonOperator.Geq: TermCase(renamed, deleted, term, "", delColor, renColor, string.Format(" size greater or equal than "), editScript, sb); break;
                                case PDLComparisonOperator.Le: TermCase(renamed, deleted, term, "", delColor, renColor, string.Format(" size smaller than "), editScript, sb); break;
                                case PDLComparisonOperator.Leq: TermCase(renamed, deleted, term, "", delColor, renColor, string.Format(" size smaller or equal than "), editScript, sb); break;
                                default: throw new PDLException("Undefined operator");
                            }
                            TermCase(renamed, deleted, term, "n", delColor, renColor, n.ToString(), editScript, sb);
                            TermCase(renamed, deleted, term, "", delColor, renColor, " elements ", editScript, sb);
                        });
                }).

                Case<PDLNot, PDLPred>((phi) =>
                {
                    if(!negate)
                        TermCase(renamed, deleted, term, delColor, renColor, "it is not the case that ", sb);                    
                    ToHTMLColoredEnglishStringInt(renamed, deleted, phi, delColor, renColor, editScript, sb, varName);
                }).

                Case<PDLPosConstant, PDLPosConstantName>((op) =>
                {
                    switch (op)
                    {
                        case PDLPosConstantName.First: TermCase(renamed, deleted, term, delColor, renColor, "the first position", sb); break;
                        case PDLPosConstantName.Last: TermCase(renamed, deleted, term, delColor, renColor, "the last position", sb); break;
                        default: throw new PDLException("undefined operator");
                    }
                }).

                Case<PDLPosUnary, PDLPos, PDLPosUnaryConstructor>((pos, op) =>
                {
                    switch (op)
                    {
                        case PDLPosUnaryConstructor.Pred: TermCase(renamed, deleted, term, delColor, renColor, "the position before ", sb); break;
                        case PDLPosUnaryConstructor.Succ: TermCase(renamed, deleted, term, delColor, renColor, "the position after ", sb); break;
                        default: throw new PDLException("undefined operator");
                    }
                    ToHTMLColoredEnglishStringInt(renamed, deleted, pos, delColor, renColor, editScript, sb, varName);
                }).

                Case<PDLPosVar, string>((var) => TermCase(renamed, deleted, term, delColor, renColor, var, sb)).

                Case<PDLPredSet, string, PDLPred>((var, phi) =>
                {
                    Pattern.Match(phi).
                        Case<PDLModSetEq, PDLSet, int, int>((set, m, n) =>
                        {
                            var cast = set as PDLAllPosUpto;
                            if (m == 2 && cast != null && cast.pos is PDLPosVar)
                            {
                                TermCase(renamed, deleted, set, delColor, renColor, "the set of ", sb);
                                TermCase(renamed, deleted, term, "n", delColor, renColor, (n == 1 ? "odd" : "even"), editScript, sb);
                                TermCase(renamed, deleted, set, delColor, renColor, "positions", sb);
                            }
                            else
                            {
                                TermCase(renamed, deleted, term, delColor, renColor, "the set {" + var + " | such that ", sb);
                                ToHTMLColoredEnglishStringInt(renamed, deleted, phi, delColor, renColor, editScript, sb, varName);
                                TermCase(renamed, deleted, term, delColor, renColor, "}", sb);
                            }
                        }).
                        Default(() =>
                        {
                            TermCase(renamed, deleted, term, delColor, renColor, "the set {" + var + " | such that ", sb);
                            ToHTMLColoredEnglishStringInt(renamed, deleted, phi, delColor, renColor, editScript, sb, varName);
                            TermCase(renamed, deleted, term, delColor, renColor, "}", sb);
                        });
                }).

                Case<PDLSetBinary, PDLSet, PDLSet, PDLBinarySetOperator>((set1, set2, op) =>
                {
                    switch (op)
                    {
                        case PDLBinarySetOperator.Intersection:
                            {
                                TermCase(renamed, deleted, term, delColor, renColor, "the intersection of ", sb); break;
                            }
                        case PDLBinarySetOperator.Union:
                            {
                                TermCase(renamed, deleted, term, delColor, renColor, "the union of ", sb); break;
                            }
                        default: throw new PDLException("undefined operator");
                    }
                    ToHTMLColoredEnglishStringInt(renamed, deleted, set1, delColor, renColor, editScript, sb, varName);
                    TermCase(renamed, deleted, term, delColor, renColor, ", and ", sb);
                    ToHTMLColoredEnglishStringInt(renamed, deleted, set2, delColor, renColor, editScript, sb, varName);
                    TermCase(renamed, deleted, term, delColor, renColor, ", ", sb);

                }).

                Case<PDLSetCmpPos, PDLPos, PDLComparisonOperator>((pos1, op) =>
                {
                    switch (op)
                    {
                        case PDLComparisonOperator.Ge: TermCase(renamed, deleted, term, delColor, renColor, "the set of positions after ", sb); break;
                        case PDLComparisonOperator.Geq: TermCase(renamed, deleted, term, delColor, renColor, "the set of positions from ", sb); break;
                        case PDLComparisonOperator.Le: TermCase(renamed, deleted, term, delColor, renColor, "the set of positions before ", sb); break;
                        case PDLComparisonOperator.Leq: TermCase(renamed, deleted, term, delColor, renColor, "the set of positions up to ", sb); break;
                        default: throw new PDLException("undefined operator");
                    }
                    ToHTMLColoredEnglishStringInt(renamed, deleted, pos1, delColor, renColor, editScript, sb, varName);
                }).

                Case<PDLSetVar, string>((var) => TermCase(renamed, deleted, term, delColor, renColor, var, sb)).

                Case<PDLSetModuleComparison, PDLSet, int, int, PDLComparisonOperator>((set, m, n, op) =>
                {
                    if (m == 2)
                    {
                        //CASE mod 2
                        Pattern.Match(set).
                            Case<PDLAllPos>(() =>
                            {
                                switch (op)
                                {
                                    case PDLComparisonOperator.Eq:
                                        {
                                            if(negate)
                                                TermCase(renamed, deleted, term, "", delColor, renColor, string.Format("<i>{0}</i> doesn't have ", varName), editScript, sb);
                                            else
                                                TermCase(renamed, deleted, term, "", delColor, renColor, string.Format("<i>{0}</i> has ", varName), editScript, sb);
                                            TermCase(renamed, deleted, term, "n", delColor, renColor, (n == 1 ? "odd" : "even"), editScript, sb);
                                            TermCase(renamed, deleted, set, "", delColor, renColor, string.Format(" length", varName), editScript, sb);
                                            break;
                                        }
                                    default: throw new PDLException("This formula shouldn't be enumerated with this operator and value 2");
                                }
                            }).
                            Case<PDLIndicesOf, string>((str) =>
                            {
                                TermCase(renamed, deleted, term, "", delColor, renColor, string.Format("'"), editScript, sb);
                                TermCase(renamed, deleted, set, "str", delColor, renColor, str, editScript, sb);
                                if(negate)
                                    TermCase(renamed, deleted, term, "", delColor, renColor, string.Format("' doesn't appear in <i>{0}</i> an ", varName), editScript, sb);
                                else
                                    TermCase(renamed, deleted, term, "", delColor, renColor, string.Format("' appears in <i>{0}</i> an ", varName), editScript, sb);

                                TermCase(renamed, deleted, term, "n", delColor, renColor, (n == 1 ? "odd" : "even"), editScript, sb);
                                TermCase(renamed, deleted, term, "", delColor, renColor, string.Format(" number of times"), editScript, sb);
                            }).
                            Default(() =>
                            {
                                switch (op)
                                {
                                    case PDLComparisonOperator.Eq:
                                        {
                                            ToHTMLColoredEnglishStringInt(renamed, deleted, set, delColor, renColor, editScript, sb, varName);
                                            if(negate)
                                                TermCase(renamed, deleted, term, "", delColor, renColor, string.Format(" doesn't have an ", varName), editScript, sb);
                                            else
                                                TermCase(renamed, deleted, term, "", delColor, renColor, string.Format(" has an ", varName), editScript, sb);

                                            TermCase(renamed, deleted, term, "n", delColor, renColor, (n == 1 ? "odd" : "even"), editScript, sb);
                                            TermCase(renamed, deleted, term, "", delColor, renColor, string.Format(" number of elements", varName), editScript, sb);
                                            break;
                                        }
                                    default: throw new PDLException("This formula shouldn't be enumerated with this operator and value 2");
                                }
                            });
                    }
                    else
                    {
                        if (n == 0 && op == PDLComparisonOperator.Eq)
                        {
                            //CASE % m = 0
                            Pattern.Match(set).
                                Case<PDLAllPos>(() =>
                                {
                                    TermCase(renamed, deleted, term, "", delColor, renColor, string.Format("the "), editScript, sb);
                                    TermCase(renamed, deleted, set, "", delColor, renColor, string.Format("length", varName), editScript, sb);
                                    if (negate)
                                        TermCase(renamed, deleted, term, "", delColor, renColor, string.Format(" of <i>{0}</i> is not divisible by ", varName), editScript, sb);
                                    else
                                        TermCase(renamed, deleted, term, "", delColor, renColor, string.Format(" of <i>{0}</i> is divisible by ", varName), editScript, sb);
                                    TermCase(renamed, deleted, term, "m", delColor, renColor, m.ToString(), editScript, sb);
                                }).
                                Case<PDLIndicesOf, string>((str) =>
                                {
                                    TermCase(renamed, deleted, term, "", delColor, renColor, string.Format("the number of times "), editScript, sb);
                                    TermCase(renamed, deleted, set, "", delColor, renColor, string.Format("the substring '"), editScript, sb);
                                    TermCase(renamed, deleted, set, "str", delColor, renColor, str, editScript, sb);
                                    TermCase(renamed, deleted, set, "", delColor, renColor, string.Format("' appears in <i>{0}</i> ", varName), editScript, sb);
                                    if (negate)
                                        TermCase(renamed, deleted, term, "", delColor, renColor, string.Format("is not divisible by ", varName), editScript, sb);
                                    else
                                        TermCase(renamed, deleted, term, "", delColor, renColor, string.Format("is divisible by ", varName), editScript, sb);
                                    TermCase(renamed, deleted, term, "m", delColor, renColor, m.ToString(), editScript, sb);
                                }).
                                Default(() =>
                                {
                                    TermCase(renamed, deleted, term, "", delColor, renColor, string.Format("the size of "), editScript, sb);
                                    ToHTMLColoredEnglishStringInt(renamed, deleted, set, delColor, renColor, editScript, sb, varName);
                                    if (negate)
                                        TermCase(renamed, deleted, term, "", delColor, renColor, string.Format(" is not divisible by "), editScript, sb);
                                    else
                                        TermCase(renamed, deleted, term, "", delColor, renColor, string.Format(" is divisible by ", varName), editScript, sb);
                                    TermCase(renamed, deleted, term, "m", delColor, renColor, m.ToString(), editScript, sb);
                                });
                        }
                        else
                        {
                            // m!=2 and n!=0 and op not equality
                            Pattern.Match(set).
                                Case<PDLAllPos>(() =>
                                {
                                    TermCase(renamed, deleted, term, "", delColor, renColor, string.Format("dividing ", varName), editScript, sb);
                                    TermCase(renamed, deleted, set, "", delColor, renColor, string.Format("the length of <i>{0}</i> by ", varName), editScript, sb);
                                    TermCase(renamed, deleted, term, "m", delColor, renColor, m.ToString(), editScript, sb);
                                }).
                                Case<PDLIndicesOf, string>((str) =>
                                {
                                    TermCase(renamed, deleted, term, "", delColor, renColor, string.Format("dividing "), editScript, sb);
                                    TermCase(renamed, deleted, set, "", delColor, renColor, string.Format("the number of times the substring '"), editScript, sb);
                                    TermCase(renamed, deleted, set, "str", delColor, renColor, str, editScript, sb);
                                    TermCase(renamed, deleted, set, "", delColor, renColor, string.Format("' appears in <i>{0}</i> ", varName), editScript, sb);
                                    TermCase(renamed, deleted, term, "", delColor, renColor, string.Format("by "), editScript, sb);
                                    TermCase(renamed, deleted, term, "m", delColor, renColor, m.ToString(), editScript, sb);
                                }).
                                Default(() =>
                                {
                                    TermCase(renamed, deleted, term, "", delColor, renColor, string.Format("dividing the size of "), editScript, sb);
                                    ToHTMLColoredEnglishStringInt(renamed, deleted, set, delColor, renColor, editScript, sb, varName);
                                    TermCase(renamed, deleted, term, "", delColor, renColor, string.Format(" by "), editScript, sb);
                                    TermCase(renamed, deleted, term, "m", delColor, renColor, m.ToString(), editScript, sb);
                                });

                            if (negate)
                                TermCase(renamed, deleted, term, "", delColor, renColor, string.Format(" doesn't give remainder "), editScript, sb);
                            else
                                TermCase(renamed, deleted, term, "", delColor, renColor, string.Format(" gives remainder "), editScript, sb);
                            switch (op)
                            {
                                case PDLComparisonOperator.Eq: break;
                                case PDLComparisonOperator.Ge: TermCase(renamed, deleted, term, "", delColor, renColor, string.Format(" greater than "), editScript, sb); break;
                                case PDLComparisonOperator.Geq: TermCase(renamed, deleted, term, "", delColor, renColor, string.Format(" greater or equal than "), editScript, sb); break;
                                case PDLComparisonOperator.Le: TermCase(renamed, deleted, term, "", delColor, renColor, string.Format(" smaller than "), editScript, sb); break;
                                case PDLComparisonOperator.Leq: TermCase(renamed, deleted, term, "", delColor, renColor, string.Format(" smaller or equal than "), editScript, sb); break;
                                default: throw new PDLException("Undefined operator");
                            }
                            TermCase(renamed, deleted, term, "n", delColor, renColor, n.ToString(), editScript, sb);
                        }
                    }
                }).

                Case<PDLStringPos, string, PDLStringPosOperator>((str, op) =>
                {
                    switch (op)
                    {
                        case PDLStringPosOperator.FirstOcc: TermCase(renamed, deleted, term, "", delColor, renColor, string.Format("the first occurence of ", str), editScript, sb); break;
                        case PDLStringPosOperator.LastOcc: TermCase(renamed, deleted, term, "", delColor, renColor, string.Format("the last occurence of ", str), editScript, sb); break;
                        default: throw new PDLException("undefined operator");
                    }
                    TermCase(renamed, deleted, term, "", delColor, renColor, "'", editScript, sb);
                    TermCase(renamed, deleted, term, "str", delColor, renColor, str, editScript, sb);
                    TermCase(renamed, deleted, term, "", delColor, renColor, "'", editScript, sb);
                }).

                Case<PDLStringQuery, string, PDLStringQueryOp>((str, op) =>
                {
                    TermCase(renamed, deleted, term, "", delColor, renColor, string.Format("s "), editScript, sb);
                    switch (op)
                    {
                        case PDLStringQueryOp.Contains: 
                            if(negate)
                                TermCase(renamed, deleted, term, "", delColor, renColor, "doesn't contain ", editScript, sb); 
                            else
                                TermCase(renamed, deleted, term, "", delColor, renColor, "contains '", editScript, sb); 
                            break;

                        case PDLStringQueryOp.EndsWith: 
                            if(negate)
                                TermCase(renamed, deleted, term, "", delColor, renColor, "doesn't end with ", editScript, sb); 
                            else
                                TermCase(renamed, deleted, term, "", delColor, renColor, "ends with ", editScript, sb); 
                            break;
                        case PDLStringQueryOp.IsString: 
                            if(negate)
                                TermCase(renamed, deleted, term, "", delColor, renColor, "isn't the string ", editScript, sb); 
                            else
                                TermCase(renamed, deleted, term, "", delColor, renColor, "is the string ", editScript, sb); 
                            break;
                        case PDLStringQueryOp.StartsWith: 
                            if(negate)
                                TermCase(renamed, deleted, term, "", delColor, renColor, "doesn't start with ", editScript, sb); 
                            else
                                TermCase(renamed, deleted, term, "", delColor, renColor, "starts with ", editScript, sb); 
                            break;
                        default: throw new PDLException("undefined operator");
                    }
                    TermCase(renamed, deleted, term, "", delColor, renColor, "'", editScript, sb);
                    TermCase(renamed, deleted, term, "str", delColor, renColor, str, editScript, sb);
                    TermCase(renamed, deleted, term, "", delColor, renColor, "'", editScript, sb);
                }).

                Case<PDLTrue>(() => TermCase(renamed, deleted, term, delColor, renColor, "true", sb));
        }

        private void TermCase(HashSet<PDL> renamed, HashSet<PDL> deleted, PDL phi, string delColor, string renColor, string message, StringBuilder sb)
        {
            if (renamed.Contains(phi))
                sb.AppendFormat("<font color='{0}'><b>{1}</b></font>", renColor, message);
            else
                if (deleted.Contains(phi))
                    sb.AppendFormat("<i><font color='{0}'><b>{1}</b></font></i>", delColor, message);
                else
                    sb.Append(message);
        }

        private void TermCase(HashSet<PDL> renamed, HashSet<PDL> deleted, PDL phi, string tag, string delColor, string renColor, string message, TreeEditScript editScript, StringBuilder sb)
        {
            var isDel = deleted.Contains(phi);
            var isRen = renamed.Contains(phi);
            foreach (var edit in editScript.script)
            {
                if (edit.getCost() > 0)
                {
                    if (isDel && edit is Delete)
                    {
                        var tmp = edit as Delete;
                        if (tmp.pdlA.First == phi && tmp.pdlA.Second == tag)
                        {
                            if(tag== "")
                                sb.AppendFormat("<font color='{0}'><b>{1}</b></font>", delColor, message);
                            else
                                sb.AppendFormat("<i><font color='{0}'><b>{1}</b></font></i>", delColor, message);
                            return;
                        }
                    }
                    else
                        if (isRen && edit is Rename)
                        {
                            var tmp = edit as Rename;
                            if (tmp.pdlA.First == phi && tmp.pdlA.Second == tag)
                            {
                                if (tag == "")
                                    sb.AppendFormat("<font color='{0}'><b>{1}</b></font>", renColor, message);
                                else
                                    sb.AppendFormat("<i><font color='{0}'><b>{1}</b></font></i>", renColor, message);                                
                                return;
                            }
                        }
                }

            }
            if (tag == "")
                sb.AppendFormat("{0}", message);
            else
                sb.AppendFormat("<i>{0}</i>", message);   
        }
    }


    #region Edit operations
    public class TreeEditScript{
        public List<EditOperation> script;

        public TreeEditScript()
        {
            this.script = new List<EditOperation>();
        }
        public TreeEditScript(TreeEditScript teScript){
            this.script = new List<EditOperation>(teScript.script);
        }
        public TreeEditScript(List<EditOperation> script)
        {
            this.script = script;
        }
        public double GetCost()
        {
            double cost = 0;
            foreach(var edit in script)
                cost+= edit.getCost();
            return cost;
        }
        public void Insert(EditOperation edit){
            script.Insert(0, edit);
        }
        public override string ToString()
        {
            string output="";
            foreach (var edit in script)
                if (edit.getCost() > 0)
                    output += string.Format("{0}; ",edit);
            return output;
        }
    }


    public abstract class EditOperation
    {
        public abstract double getCost();
    }

    public class Delete : EditOperation
    {
        internal string labelA;
        internal Pair<PDL, string> pdlA;        

        public Delete(int aNodeId, TreeDefinition aTree)
        {
            labelA = aTree.getLabelForMatching(aNodeId);

            pdlA = aTree.pdlNodeMapping[labelA];

            int div = labelA.LastIndexOf(":");
            if (div == -1)
                throw new PDLException(string.Format("All nodes should have a : in the middle: {0}", labelA));

            labelA = labelA.Substring(0, div);
        }

        public override double getCost()
        {
            return 1;
        }
        public override string ToString()
        {
            return "delete " + labelA;
        }
    }

    public class Insert : EditOperation
    {
        internal string labelB;
        internal Pair<PDL,string> pdlB;

        public Insert(int bNodeId, TreeDefinition bTree)
        {
            labelB = bTree.getLabelForMatching(bNodeId);

            pdlB = bTree.pdlNodeMapping[labelB];

            int div = labelB.LastIndexOf(":");
            if (div == -1)
                throw new PDLException(string.Format("All nodes should have a : in the middle: {0}", labelB));            

            labelB = labelB.Substring(0, div);
        }

        public override double getCost()
        {
            return 1;
        }
        public override string ToString()
        {
            return "insert "+labelB;
        }
    }

    public class Rename : EditOperation
    {
        internal string labelA;
        internal string labelB;
        internal Pair<PDL, string> pdlA;
        internal Pair<PDL, string> pdlB;

        public Rename(int aNodeId, int bNodeId, TreeDefinition aTree, TreeDefinition bTree)
        {
            labelA = aTree.getLabelForMatching(aNodeId);
            labelB = bTree.getLabelForMatching(bNodeId);

            pdlA = aTree.pdlNodeMapping[labelA];
            pdlB = bTree.pdlNodeMapping[labelB];

            int aDiv = labelA.LastIndexOf(":");
            if (aDiv == -1)
                throw new PDLException(string.Format("All nodes should have a : in the middle: {0}", labelA));
            int bDiv = labelB.LastIndexOf(":");
            if (bDiv == -1)
                throw new PDLException(string.Format("All nodes should have a : in the middle: {0}", labelB));

            labelA = labelA.Substring(0, aDiv);
            labelB = labelB.Substring(0, bDiv);
        }

        public override double getCost()
        {
            return labelA == labelB ? 0 : 1;
        }

        public override string ToString()
        {
            return string.Format("rename {0} to {1}", labelA, labelB);
        }
    }
    #endregion

    #region Tree Definitions
    public abstract class TreeDefinition
    {

        //Just two constants for defining ordering.
        public const int POSTORDER = 0;
        public const int PREORDER = 1;
        private int chosenOrder = 0;

        //root node label
        internal String root = "";
        internal PDLPred pdl;

        private Dictionary<int, String> iDsToLabel = new Dictionary<int, String>();
        private Dictionary<String, int> labelToIDs = new Dictionary<String, int>();
        private Dictionary<int, List<int>> treeStructureIDs = new Dictionary<int, List<int>>();

        public Dictionary<string, Pair<PDL, string>> pdlNodeMapping = new Dictionary<string, Pair<PDL, string>>();

        /** Returns the NodeID of the root */
        public int getRootID()
        {
            return labelToIDs[root];
        }

        /** This is the ordering used to number the nodes */
        public void setOrder(int order)
        {
            chosenOrder = order;
        }

        /** This is the ordering used to number the nodes */
        public int getOrder()
        {
            return chosenOrder;
        }

        public void orderNodes(int ordering)
        {
            //Set ordering
            if (ordering == POSTORDER)
            {
                setPostOrdering(0, root);
                setOrder(POSTORDER);
            }
            else
            { //PREORDER
                setOrder(PREORDER);
            }
            //Create version of tree that just uses index numbers
            //
            foreach (var parent in getNodes())
            {
                List<int> indexedChildren = new List<int>();
                foreach (String child in getChildren(parent))
                    indexedChildren.Add(getNodeID(child));

                treeStructureIDs[getNodeID(parent)] =
                         indexedChildren;
            }
        }

        public int setPostOrdering(int counter, String aNodeLabel)
        {
            int internalCounter = counter;

            //examine children
            foreach (String child in getChildren(aNodeLabel))
            {
                internalCounter = setPostOrdering(internalCounter, child);
            }

            //set new nodeID for this node (set to counter+1)
            putLabel(internalCounter + 1, aNodeLabel);
            putNodeID(aNodeLabel, internalCounter + 1);

            return internalCounter + 1;
        }


        /** This is provides the string label for the node we're matching.
         * In some cases, the value of the string may depend on properties
         * of the node in addition to the actual node label. */
        public String getLabelForMatching(int nodeID)
        {
            return getLabel(nodeID);
        }

        public String getLabel(int nodeID)
        {
            return iDsToLabel[nodeID];
        }

        public int getNodeID(String nodeLabel)
        {
            return labelToIDs[nodeLabel];
        }

        public void putLabel(int nodeID, String nodeLabel)
        {
            iDsToLabel[nodeID] = nodeLabel;
        }

        public void putNodeID(String nodeLabel, int nodeID)
        {
            labelToIDs[nodeLabel] = nodeID;
        }

        /* returns a IEnumerable of nodes referred to by their original
         * (unique) nodel label.
         */
        public abstract IEnumerable<String> getNodes();

        /* returns a IEnumerable of children referred to by their original
         * (unique) nodel label.
         */
        public abstract List<String> getChildren(String aNodeLabel);

        /** Returns the children of the node given as a parameter. */
        public IEnumerable<int> getChildrenIDs(int nodeID)
        {
            return treeStructureIDs[nodeID];
        }
        public bool isLeaf(int nodeID)
        {
            return (getChildrenIDs(nodeID).ToList().Count == 0);
        }

        public int getNodeCount()
        {
            return treeStructureIDs.Keys.Count;
        }

        public abstract String toString();
    }

    public class BasicTree : TreeDefinition
    {

        //A set of nodes (once set, this is not changed)
        public Dictionary<string, List<String>> treeStructure = new Dictionary<String, List<String>>();

        public BasicTree()
        {
        }

        /** This takes a |e| x 2 array of string, where |e| is the number
         * of edges.
         */
        public BasicTree(Dictionary<String, List<String>> tree, String root, int ordering)
        {
            this.root=root;
            treeStructure = tree;

            orderNodes(ordering);
        }

        /** Returns the parent nodes in the tree*/
        public override IEnumerable<String> getNodes()
        {
            return treeStructure.Keys;
        }

        /** Returns the children of the node given as a parameter. */
        public override List<String> getChildren(String nodeLabel)
        {
            return treeStructure[nodeLabel];
        }

        public override String toString()
        {
            int rootID = getRootID();
            StringBuilder rStr = new StringBuilder();

            for (int i = rootID; i > 0; i--)
            {
                rStr.Append(getLabel(i) + "(" + i + ") \n");
                foreach (String child in getChildren(getLabel(i)))
                {
                    rStr.Append(" - " + child + "(" + getNodeID(child) + ")  \n");
                }
            }
            return rStr.ToString();
        }
    } 
    #endregion


    public static class CreateTreeHelper
    {
        /* This takes a String describing a tree and converts it into a
         * TreeDefinition.  The format of the string is a series of edges
         * represented as pairs of string separated by semi-colons.  Each
         * pair is comma separated.  The first substring in the pair is
         * the parent, the second is the child.  The first edge parent
         * must be the root of the tree.  
         *
         * For example: "a-b;a-c;c-d;c-e;c-f;"
         */
        public static TreeDefinition MakeTree(PDLPred phi)
        {
            Dictionary<string, Pair<PDL, string>> dic = new Dictionary<string,Pair<PDL,string>>();
            string pdlTreeString = phi.ToTreeString(dic);
            return MakeTree(dic, pdlTreeString, null, phi);
        }

        public static TreeDefinition MakeTree(Dictionary<string, Pair<PDL, string>> nodeMapping, String treeSpec, String rootID, PDLPred pdl)
        {
            Dictionary<String, List<String>> aTree
                = new Dictionary<String, List<String>>();

            String root = rootID;

            String[] edges = treeSpec.Split(';');
            foreach (String edge in edges)
                if (edge != "")
                {
                    String[] nodes = edge.Split('-');
                    addEdge(nodes[0], nodes[1], aTree);
                    if (root == null)
                        root = nodes[0];
                    
                }

            BasicTree aBasicTree = new BasicTree(aTree, root, BasicTree.POSTORDER);
            aBasicTree.pdlNodeMapping = nodeMapping;
            aBasicTree.pdl = pdl;

            return aBasicTree;
        }

        /** This adds the edge (and nodes if necessary) to the tree
         * definition .
         */
        internal static void addEdge(String parentLabel, String childLabel,
                 Dictionary<String, List<String>> treeStructure)
        {
            //Add Parent node, edge and child
            if (!treeStructure.ContainsKey(parentLabel))
                treeStructure[parentLabel] = new List<String>();

            treeStructure[parentLabel].Add(childLabel);

            //Add child if not already there
            if (!treeStructure.ContainsKey(childLabel))
                treeStructure[childLabel] = new List<String>();            
        }


    }

    public static class PDLUtil{

        public static string ToEnglishString(PDLPred phi)
        {
            Transformation t = new Transformation();
            return t.ToEnglishString(phi);
        }
    }

}
