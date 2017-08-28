using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Services;
using System.Xml.Linq;
using System.Text;

using Microsoft.Automata;

namespace AutomataPDL.Utilities
{
    public class BooleanOperation
    {
        //Todo: Better system for parsing Strings.
        //-1...NOT (only 'left' is used)
        //0...AND
        //1...OR
        //2...IF
        //3...IFF
        //42...Terminal
        private int operation;

        private BooleanOperation left;
        private BooleanOperation right;

        private int content;

        public BooleanOperation(BooleanOperation l, BooleanOperation r, int op, int cont) {
            left = l;
            right = r;
            operation = op;
            content = cont;
        }

        public bool IsTrueForInterpretation(bool[] interpretation)
        {
            switch (operation) {
                case -1:
                    return !left.IsTrueForInterpretation(interpretation);
                case 0:
                    return left.IsTrueForInterpretation(interpretation) && right.IsTrueForInterpretation(interpretation);
                case 1:
                    return left.IsTrueForInterpretation(interpretation) || right.IsTrueForInterpretation(interpretation);
                case 2:
                    return !left.IsTrueForInterpretation(interpretation) || right.IsTrueForInterpretation(interpretation);
                case 3:
                    bool a = left.IsTrueForInterpretation(interpretation);
                    bool b = right.IsTrueForInterpretation(interpretation);
                    return (!a && !b) || (a && b);
                case 42:
                    return interpretation[content];
            }

            return false;
        }

        public Automaton<BDD> executeOperationOnAutomataList(List<Automaton<BDD>> automataList, CharSetSolver solver)
        {
            if (matchesAutomataList(automataList))
                return executeOnAutomataList(automataList, solver);

            return null;
        }

        private bool matchesAutomataList(List<Automaton<BDD>> automataList)
        {
            switch (operation) {
                case -1:
                    return left.matchesAutomataList(automataList);
                case 0:
                case 1:
                case 2:
                case 3:
                    return left.matchesAutomataList(automataList) && right.matchesAutomataList(automataList);
                case 42:
                    return content >= 0 && content < automataList.Count();
            }

            return false;
        }

        private Automaton<BDD> executeOnAutomataList(List<Automaton<BDD>> automataList, CharSetSolver solver)
        {
            switch (operation) {
                case -1:
                    return left.executeOnAutomataList(automataList, solver).Complement(solver).Minimize(solver);
                case 0:
                    return left.executeOnAutomataList(automataList, solver).Intersect(right.executeOnAutomataList(automataList, solver), solver).Determinize(solver).Minimize(solver);
                case 1:
                    return left.executeOnAutomataList(automataList, solver).Union(right.executeOnAutomataList(automataList, solver), solver).Determinize(solver).Minimize(solver);
                case 2:
                    return left.executeOnAutomataList(automataList, solver).Complement(solver).Union(right.executeOnAutomataList(automataList, solver), solver).Determinize(solver).Minimize(solver);
                case 3:
                    return left.executeOnAutomataList(automataList, solver).Intersect(right.executeOnAutomataList(automataList, solver), solver).Union(
                        left.executeOnAutomataList(automataList, solver).Complement(solver).Intersect((right.executeOnAutomataList(automataList, solver)).Complement(solver), solver)).Determinize(solver).Minimize(solver);
                case 42:
                    if(content < automataList.Count())
                        return automataList[content];

                    return null;
            }

            return null;
        }

        //TODO: Variables start from 0
        public static BooleanOperation parseBooleanOperationFromXML(XElement boolOp)
        {
            XElement xelem = XElement.Parse(RemoveAllNamespaces(boolOp.ToString()));
            string s = xelem.Value;

            if (isSyntacticlyCorrectOperation(s))
                return parseBooleanOperationFromString(s);

            return null;
        }
        
        public static BooleanOperation parseBooleanOperationFromString(string op)
        {
            int bracketLevel = 0;
            int highestOperation = -1;
            int highestOperationIndex = -1;

            bool onlyNumbers = true;

            for (int i = 0; i < op.Length; i++) {
                char c = op.ElementAt(i);
                if (!"0123456789".Contains(c.ToString()))
                    onlyNumbers = false;

                if (c == '(') {
                    bracketLevel++;
                }
                if (bracketLevel == 0) {
                    if (c == '&' && highestOperation < 0) {
                        highestOperation = 0;
                        highestOperationIndex = i;
                    }
                    else if (c == '+' && highestOperation < 1)
                    {
                        highestOperation = 1;
                        highestOperationIndex = i;
                    }
                    else if (c == '=' && highestOperation < 2)
                    {
                        highestOperation = 2;
                        highestOperationIndex = i;
                        i++;
                    }
                    else if (c == '<' && highestOperation < 3)
                    {
                        highestOperation = 3;
                        highestOperationIndex = i;
                        i += 2;
                    }
                }
                if (c == ')')
                {
                    bracketLevel--;
                }
            }

            if (onlyNumbers)
                return new BooleanOperation(null, null, 42, Int32.Parse(op));

            switch (highestOperation) {
                case 0:
                case 1:
                    return new BooleanOperation(parseBooleanOperationFromString(op.Substring(0, highestOperationIndex)),
                                                parseBooleanOperationFromString(op.Substring(highestOperationIndex + 1)),
                                                highestOperation, -1);
                case 2:
                    return new BooleanOperation(parseBooleanOperationFromString(op.Substring(0, highestOperationIndex)),
                                                parseBooleanOperationFromString(op.Substring(highestOperationIndex + 2)),
                                                highestOperation, -1);
                case 3:
                    return new BooleanOperation(parseBooleanOperationFromString(op.Substring(0, highestOperationIndex)),
                                                parseBooleanOperationFromString(op.Substring(highestOperationIndex + 3)),
                                                highestOperation, -1);
                default:
                    if (op.ElementAt(0) == '!') {
                        return new BooleanOperation(parseBooleanOperationFromString(op.Substring(1)), null, highestOperation, -1);
                    }

                    return parseBooleanOperationFromString(op.Substring(1, op.Length - 2));
            }
        }

        private static bool isSyntacticlyCorrectOperation(string op)
        {
            return true;
        }

        #region XML parsing helpers
        public static string RemoveAllNamespaces(string xmlDocument)
        {
            XElement xmlDocumentWithoutNs = RemoveAllNamespaces(XElement.Parse(xmlDocument));

            return xmlDocumentWithoutNs.ToString();
        }

        private static XElement RemoveAllNamespaces(XElement xmlDocument)
        {
            if (!xmlDocument.HasElements)
            {
                XElement xElement = new XElement(xmlDocument.Name.LocalName);
                xElement.Value = xmlDocument.Value;

                foreach (XAttribute attribute in xmlDocument.Attributes())
                {
                    if (!attribute.IsNamespaceDeclaration)
                    {
                        xElement.Add(attribute);
                    }
                }

                return xElement;
            }
            return new XElement(xmlDocument.Name.LocalName, xmlDocument.Elements().Select(el => RemoveAllNamespaces(el)));
        }
        #endregion

    }
}
