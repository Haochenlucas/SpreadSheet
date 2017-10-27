/****************************************************************************
* dll - Spreadsheet
* Author:   Benwei Shi (u1088102), Student of University of Utah
* Date:     10/01/2016
* Purpose:  Assignment PS5 of CS 3500
* Usage:    enhancing and extending PS4
****************************************************************************/
using SpreadsheetUtilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;

namespace SS
{
    /// <summary>
    /// Thrown to indicate that a change to a cell will cause a circular dependency.
    /// </summary>
    public class CircularException : Exception
    {
    }


    /// <summary>
    /// Thrown to indicate that a name parameter was either null or invalid.
    /// </summary>
    public class InvalidNameException : Exception
    {
    }


    // ADDED FOR PS5
    /// <summary>
    /// Thrown to indicate that a read or write attempt has failed.
    /// </summary>
    public class SpreadsheetReadWriteException : Exception
    {
        /// <summary>
        /// Creates the exception with a message
        /// </summary>
        public SpreadsheetReadWriteException(string msg)
            : base(msg)
        {
        }
    }

    /// <summary>
    /// This is an implement of abstractSpreadsheet, represents the state of a 
    /// simple spreadsheet. A spreadsheet consists of an infinite number of named 
    /// cells.
    /// 
    /// The contents of a cell can be (1) a string, (2) a double, or (3) a Formula.  
    /// In a new spreadsheet, the contents of every cell is the empty string.
    /// 
    /// The value of a cell can be (1) a string, (2) a double, or (3) a FormulaError.  
    /// If a cell's contents is a string, its value is that string.
    /// If a cell's contents is a double, its value is that double.
    /// 
    /// If a cell's contents is a Formula, its value is either a double or a FormulaError,
    /// as reported by the Evaluate method of the Formula class.  The value of a Formula,
    /// of course, can depend on the values of variables.  The value of a variable is the 
    /// value of the spreadsheet cell it names (if that cell's value is a double) or 
    /// is undefined (otherwise).
    /// 
    /// Spreadsheets are never allowed to contain a combination of Formulas that establish
    /// a circular dependency.
    /// </summary>
    public class Spreadsheet
    {
        /// <summary>
        /// True if this spreadsheet has been modified since it was created or saved                  
        /// (whichever happened most recently); false otherwise.
        /// </summary>
        public bool Changed { get; set; }
        
        /// <summary>
        /// Method used to determine whether a string that consists of one or more letters
        /// followed by one or more digits is a valid variable name.
        /// </summary>
        public Func<string, bool> IsValid { get; protected set; }
        
        /// <summary>
        /// Method used to convert a cell name to its standard form.  For example,
        /// Normalize might convert names to upper case.
        /// </summary>
        public Func<string, string> Normalize { get; protected set; }
        
        /// <summary>
        /// Version information
        /// </summary>
        public string Version { get; protected set; }
        // A Dictionary to store all cells contents, execpt "".
        // Key for name, Value for contents.
        private Dictionary<String, object> cells;

        // A DependencyGraph for tracking the dependency of all cells
        private DependencyGraph dependency;
        
        /// <summary>
        /// zero-argument constructor should create an empty spreadsheet that imposes 
        /// no extra validity conditions, normalizes every cell name to itself, and has 
        /// version "default".
        /// </summary>
        public Spreadsheet() : this(IsValidCellName, s => s, "default")
        {
        }

        /// <summary>
        /// 3-argument constructor should create an empty spreadsheet. However, it should 
        /// allow the user to provide a validity delegate (first parameter), a 
        /// normalization delegate (second parameter), and a version (third parameter).
        /// </summary>
        /// <param name="isValid">a validity delegate</param>
        /// <param name="normalize">a normalization delegate</param>
        /// <param name="version">string of the version</param>
        public Spreadsheet(Func<string, bool> isValid, Func<string, string> normalize, string version) 
        {
            this.IsValid = isValid;
            this.Normalize = normalize;
            this.Version = version;
            dependency = new DependencyGraph();
            cells = new Dictionary<string, object>();
        }

        /// <summary>
        /// A four-argument constructor to the Spreadsheet class. 
        /// It allow the user to provide a file, a validity delegate, a normalization 
        /// delegate, and a version. 
        /// It reads a saved spreadsheet from a file (see the Save method) and use it to 
        /// construct a new spreadsheet. The new spreadsheet should use the provided 
        /// validity delegate, normalization delegate, and version.
        /// </summary>
        /// <param name="filename">a string representing a path to a file</param>
        /// <param name="isValid">a validity delegate, string to bool</param>
        /// <param name="normalize">a normalization delegate, string to string</param>
        /// <param name="version">a string of version</param>
        public Spreadsheet(string filename, Func<string, bool> isValid, Func<string, string> normalize, string version)
            : this(isValid, normalize, version)
        {
            // try to find spreadsheet in the file and make sure the version is match.
            if (GetSavedVersion(filename) != version)
                throw new SpreadsheetReadWriteException(
                    "The version of the file does no match.");

            // try to read spreadsheet saved in the given filename.
            try
            {
                using (XmlReader xmlReader = XmlReader.Create(filename))
                {
                    while (xmlReader.Read())
                    {
                        if (xmlReader.IsStartElement() && xmlReader.Name == "cell")
                        {
                            // finde a cell, try to read name and contents
                            string name = null;
                            string contents = null;
                            xmlReader.Read();
                            if (xmlReader.IsStartElement("name"))
                            {
                                xmlReader.Read();
                                name = xmlReader.Value;
                            }
                            xmlReader.Read();
                            xmlReader.Read();
                            if (xmlReader.IsStartElement("contents"))
                            {
                                xmlReader.Read();
                                contents = xmlReader.Value;
                            }
                            // if find both name and contents, Set the cell.
                            if (name != null && contents != null)
                                SetContentsOfCell(name, contents);
                            else
                                throw new XmlException();
                        }
                    }
                }
            }
            catch (Exception)
            {
                throw new SpreadsheetReadWriteException(
                    "The format does not match. The file is damaged.");
            }

            Changed = false;
        }

        /// <summary>
        /// Returns the version information of the spreadsheet saved in the named file.
        /// If there are any problems opening, reading, or closing the file, the method
        /// should throw a SpreadsheetReadWriteException with an explanatory message.
        /// </summary>
        /// <param name="filename">file path and name of the saved spreadsheet</param>
        /// <returns>Version of the spreadsheet saved in the file</returns>
        public string GetSavedVersion(string filename)
        {
            try
            {
                using (XmlReader xmlReader = XmlReader.Create(filename))
                {
                    // try to find spreadsheet from the first element
                    string version = null;
                    xmlReader.Read();
                    if (xmlReader.IsStartElement("spreadsheet"))
                        version = xmlReader.GetAttribute("version");
                    // If found version of spreadsheet, return
                    // else throw exception.
                    if (version != null)
                        return version;
                    throw new Exception();
                }
            }
            catch (ArgumentException)
            {
                throw new SpreadsheetReadWriteException(
                    "Cannot read the file with the given file name.");
            }
            catch (Exception)
            {
                throw new SpreadsheetReadWriteException(
                    "Cannot find version of spreadsheet from the given file name.");
            }
        }

        // ADDED FOR PS5
        /// <summary>
        /// Writes the contents of this spreadsheet to the named file using an XML format.
        /// The XML elements should be structured as follows:
        /// 
        /// <spreadsheet version="version information goes here">
        /// <cell>
        /// <name>
        /// cell name goes here
        /// </name>
        /// <contents>
        /// cell contents goes here
        /// </contents>    
        /// </cell>
        /// </spreadsheet>
        /// 
        /// If there are any problems opening, writing, or closing the file, the method should throw a
        /// SpreadsheetReadWriteException with an explanatory message.
        /// </summary>
        /// <param name="filename">filename to save the spreadsheet.</param>
        public void Save(string filename)
        {
            // Specifically, use indentation to make it more readable.
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.IndentChars = ("\t");

            try
            {
                using (XmlWriter xml = XmlWriter.Create(filename, settings))
                {
                    // <?xml version="1.0" encoding="utf-8"?>
                    xml.WriteStartDocument();
                    // <spreadsheet version="Version">
                    xml.WriteStartElement("spreadsheet");
                    xml.WriteAttributeString("version", Version);
                    foreach (string cell in GetNamesOfAllNonemptyCells())
                    {
                        // <cell>
                        xml.WriteStartElement("cell");
                        // <name>A1</name>
                        xml.WriteElementString("name", cell);
                        // <contents>content</contents>
                        Formula f = GetCellContents(cell) as Formula;
                        string contents;
                        if (f != null)
                            contents = "=" + f.ToString();
                        else
                            contents = GetCellContents(cell).ToString();
                        xml.WriteElementString("contents", contents);
                        // </cell>
                        xml.WriteEndElement();
                    }
                    // </spreadsheet>
                    xml.WriteEndElement();
                    xml.WriteEndDocument();
                    // Set changed to false.
                    Changed = false;
                }
            }
            catch (ArgumentException)
            {
                throw new SpreadsheetReadWriteException(
                    "Cannot create the file with the given file name.");
            }
            catch (Exception)
            {
                throw new SpreadsheetReadWriteException("Save file faild.");
            }
        }

        /// <summary>
        /// If name is null or invalid, throws an InvalidNameException.
        /// 
        /// Otherwise, returns the value (as opposed to the contents) of the named cell.  The return
        /// value should be either a string, a double, or a SpreadsheetUtilities.FormulaError.
        /// </summary>
        /// <param name="name">Name of the cell</param>
        /// <returns>Value of the cell</returns>
        public object GetCellValue(string name)
        {
            object content = GetCellContents(name);

            // try formula
            Formula f = content as Formula;
            if (f != null)
            {
                try
                {
                    return f.Evaluate(EvaluateCellValue);
                }
                catch (ArgumentException e)
                {
                    return new FormulaError(e.Message);
                }
            }
            // if not formula, must be double or string, just return as it.
            else return content;
        }

        /// <summary>
        /// If name is null or invalid, throws an InvalidNameException.
        /// Otherwise, returns the contents (as opposed to the value) of the named cell.  The return
        /// value should be either a string, a double, or a Formula.
        /// </summary>
        /// <param name="name">The name of the cell to get contents.</param>
        /// <returns>The contents of the named cell</returns>
        public object GetCellContents(string name)
        {
            // check if name is valid. and normalize name.
            name = Normalize(name);
            if (!IsValidCellName(name))
                throw new InvalidNameException();

            // Try to find cell in the cells dictionary, return it's contents.
            // If cannot find it, means it is empty, return empty string.
            object contents;
            if (cells.TryGetValue(name, out contents))
                return contents;
            else
                return "";
        }

        /// <summary>
        /// Enumerates the names of all the non-empty cells in the spreadsheet.
        /// </summary>
        /// <returns>IEnumerable strings of all none empty cells' name</returns>
        public IEnumerable<string> GetNamesOfAllNonemptyCells()
        {
            return cells.Keys;
        }

        /// <summary>
        /// If content is null, throws an ArgumentNullException.
        /// 
        /// Otherwise, if name is null or invalid, throws an InvalidNameException.
        /// 
        /// Otherwise, if content parses as a double, the contents of the named
        /// cell becomes that double.
        /// 
        /// Otherwise, if content begins with the character '=', an attempt is made
        /// to parse the remainder of content into a Formula f using the Formula
        /// constructor.  There are then three possibilities:
        /// 
        ///   (1) If the remainder of content cannot be parsed into a Formula, a 
        ///       SpreadsheetUtilities.FormulaFormatException is thrown.
        ///       
        ///   (2) Otherwise, if changing the contents of the named cell to be f
        ///       would cause a circular dependency, a CircularException is thrown.
        ///       
        ///   (3) Otherwise, the contents of the named cell becomes f.
        /// 
        /// Otherwise, the contents of the named cell becomes content.
        /// 
        /// If an exception is not thrown, the method returns a set consisting of
        /// name plus the names of all other cells whose value depends, directly
        /// or indirectly, on the named cell.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        public ISet<string> SetContentsOfCell(string name, string content)
        {
            // check if all argument is valid. and normalize name.
            if (content == null)
                throw new ArgumentNullException("The contents of the cell cannot be null");
            name = Normalize(name);
            if (!IsValid(name))
                throw new InvalidNameException();
            // Chagne will be made.
            Changed = true;

            // try double
            double v;
            if (double.TryParse(content, out v))
                return SetCellContents(name, v);

            // try formula
            if (content.Length > 0 && content[0] == '=')
                return SetCellContents(name, new Formula(content.Substring(1), Normalize, IsValid));

            // string
            return SetCellContents(name, content);

        }

        /// <summary>
        /// If the formula parameter is null, throws an ArgumentNullException.
        /// Otherwise, if name is null or invalid, throws an InvalidNameException.
        /// Otherwise, if changing the contents of the named cell to be the formula would cause a 
        /// circular dependency, throws a CircularException.  (No change is made to the spreadsheet.)
        /// 
        /// Otherwise, the contents of the named cell becomes formula.  The method returns a
        /// Set consisting of name plus the names of all other cells whose value depends,
        /// directly or indirectly, on the named cell.
        /// </summary>
        protected ISet<string> SetCellContents(string name, Formula formula)
        {
            // If the formula has error.
            SetCell(name, formula);
            if (formula.ErrorMessage.Length > 0)
            {
                // also update the dependees of the cell.
                dependency.ReplaceDependees(name, Enumerable.Empty<string>());
                return new HashSet<string>(GetCellsToRecalculate(name));
            }
            
            // try update
            dependency.ReplaceDependees(name, formula.GetVariables());
            return new HashSet<string>(GetCellsToRecalculate(name));
        }

        /// <summary>
        /// If text is null, throws an ArgumentNullException.        /// 
        /// Otherwise, if name is null or invalid, throws an InvalidNameException.
        /// 
        /// Otherwise, the contents of the named cell becomes text.  The method returns a
        /// set consisting of name plus the names of all other cells whose value depends, 
        /// directly or indirectly, on the named cell.
        /// If the contents is an empty string, we say that the cell is empty.
        /// </summary>
        /// <param name="name">Name of the cell.</param>
        /// <param name="text">Content of the cell.</param>
        /// <returns>A set of all effected cell's name</returns>
        protected ISet<string> SetCellContents(string name, string text)
        {
            // If the new content is empty, delete the cell.
            if (text == "")
                cells.Remove(name);
            else
                SetCell(name, text);
            // also update the dependees of the cell.
            dependency.ReplaceDependees(name, Enumerable.Empty<string>());
            return new HashSet<string>(GetCellsToRecalculate(name));
        }

        /// <summary>
        /// If name is null or invalid, throws an InvalidNameException.
        /// 
        /// Otherwise, the contents of the named cell becomes number.  The method returns a
        /// set consisting of name plus the names of all other cells whose value depends, 
        /// directly or indirectly, on the named cell.
        /// 
        /// For example, if name is A1, B1 contains A1*2, and C1 contains B1+A1, the
        /// set {A1, B1, C1} is returned.
        /// </summary>
        /// <param name="name">Name of the cell.</param>
        /// <param name="number">Content of the cell.</param>
        /// <returns>A set of all effected cell's name</returns>
        protected ISet<string> SetCellContents(string name, double number)
        {
            // update the content of the cell.
            SetCell(name, number);
            // also update the dependees of the cell.
            dependency.ReplaceDependees(name, Enumerable.Empty<string>());
            return new HashSet<string>(GetCellsToRecalculate(name));
        }

        /// <summary>
        /// If name is null, throws an ArgumentNullException.
        /// 
        /// Otherwise, if name isn't a valid cell name, throws an InvalidNameException.
        /// 
        /// Otherwise, returns an enumeration, without duplicates, of the names of all cells whose
        /// values depend directly on the value of the named cell.  In other words, returns
        /// an enumeration, without duplicates, of the names of all cells that contain
        /// formulas containing name.
        /// 
        /// </summary>
        /// <param name="name">The name of the cell whose direct dependents are returned.</param>
        /// <returns>Enumerable string of names of direct dependents.</returns>
        protected IEnumerable<string> GetDirectDependents(string name)
        {
            // These commeted code can never been reached so far.
            /*if (name == null)
                throw new ArgumentNullException();
            if (!IsValidCellName(name))
                throw new InvalidNameException();*/
            return dependency.GetDependents(name);
        }

        /// <summary>
        /// Check if a String is a valid cell name.
        /// A string is a valid cell name if and only if:
        ///   (1) its first character is an underscore or a letter
        ///   (2) its remaining characters (if any) are underscores and/or letters and/or digits
        /// Note that this is the same as the definition of valid variable from the PS3 Formula class.
        /// 
        /// For example, "x", "_", "x2", "y_15", and "___" are all valid cell  names, but
        /// "25", "2x", and "&" are not.  Cell names are case sensitive, so "x" and "X" are
        /// different cell names.
        /// </summary>
        /// <param name="name">The name to be check.</param>
        /// <returns></returns>
        private static bool IsValidCellName(String name)
        {
            if (name == null) return false;
            String cellNamePattern = @"^[a-zA-Z]+\d+$";
            return Regex.IsMatch(name, cellNamePattern);
        }

        /// <summary>
        /// Help method for 3 SetCellContents methods, add or update cell.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="cell"></param>
        private void SetCell(string name, object content)
        {
            // if found cell, update it, else add a new one.
            if (cells.ContainsKey(name))
                cells[name] = content;
            else
                cells.Add(name, content);
        }

        /// <summary>
        /// Return the double value of the given cell name, or throw ArgumentException.
        /// </summary>
        /// <param name="name">Name of the cell</param>
        /// <returns>double value of the cell</returns>
        private double EvaluateCellValue(string name)
        {
            // try to convert the value of cell to doulbe.
            // what is the best way to try to use an object as a double? Or get double back
            // from an object?
            string value = GetCellValue(name).ToString();
            double d = 0d;
            // Changes the value of an empty cell to 0 if evaluated
            if (value == "")
            {
                return 0;
            }
            else if (double.TryParse(value, out d))
                return d;
            else
                throw new ArgumentException("#" + name + "!");
        }

        /// <summary>
        /// Requires that names be non-null.  Also requires that if names contains s,
        /// then s must be a valid non-null cell name.
        /// 
        /// If any of the named cells are involved in a circular dependency,
        /// throws a CircularException.
        /// 
        /// Otherwise, returns an enumeration of the names of all cells whose values must
        /// be recalculated, assuming that the contents of each cell named in names has changed.
        /// The names are enumerated in the order in which the calculations should be done.  
        /// 
        /// For example, suppose that 
        /// A1 contains 5
        /// B1 contains 7
        /// C1 contains the formula A1 + B1
        /// D1 contains the formula A1 * C1
        /// E1 contains 15
        /// 
        /// If A1 and B1 have changed, then A1, B1, and C1, and D1 must be recalculated,
        /// and they must be recalculated in either the order A1,B1,C1,D1 or B1,A1,C1,D1.
        /// The method will produce one of those enumerations.
        /// 
        /// Please note that this method depends on the abstract GetDirectDependents.
        /// It won't work until GetDirectDependents is implemented correctly.
        /// </summary>
        private IEnumerable<String> GetCellsToRecalculate(ISet<String> names)
        {
            LinkedList<String> changed = new LinkedList<String>();
            HashSet<String> visited = new HashSet<String>();
            foreach (String name in names)
            {
                if (!visited.Contains(name))
                {
                    Visit(name, name, visited, changed);
                }
            }
            return changed;
        }


        /// <summary>
        /// A convenience method for invoking the other version of GetCellsToRecalculate
        /// with a singleton set of names.  See the other version for details.
        /// </summary>
        private IEnumerable<String> GetCellsToRecalculate(String name)
        {
            return GetCellsToRecalculate(new HashSet<String>() { name });
        }


        /// <summary>
        /// A helper for the GetCellsToRecalculate method.
        /// </summary>
        private void Visit(String start, String name, ISet<String> visited, LinkedList<String> changed)
        {
            visited.Add(name);
            foreach (String n in GetDirectDependents(name))
            {
                if (((Formula)cells[n]).ErrorMessage == "Circular Dependency")
                {
                    ((Formula)cells[n]).ErrorMessage = "";
                }
                
                if (n.Equals(start))
                {
                    ((Formula)cells[n]).ErrorMessage = "Circular Dependency";
                    ((Formula)cells[start]).ErrorMessage = "Circular Dependency";
                }
                else if (!visited.Contains(n))
                {
                    Visit(start, n, visited, changed);
                }
            }
            changed.AddFirst(name);
        }

    }

}
