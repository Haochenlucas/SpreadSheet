Written by Benwei Shi and Charles Clausen for CS 3500 PS6 assignment, 11/03/2016

The solution PS6 was branched from the previous assignment, PS5.  In this branch
we created the GUI for our spreadsheet.  The finished application functionality is
outlined below.

Basic Functionality:
	- New: Open a new spreadsheet without re-opening the application
	
	- Save: Save the spreadsheet to edit at a later time.  Default filetype
			is .sprd 
	
	- Save As: Choose the path and filename for a .sprd file
	
	- Open: Open a .sprd file from wherever on the machine
	
	- Exit: Close the current spreadsheet, does not close all spreadsheet windows
	
	- Cell editing: The whole point of the application.  This is done by clicking 
			the desired cell and editing the contents.  Values are updated automatically.
			Values are displayed in the cell and cell's name and value are displayed
			above the spreadsheet grid.  Valid input for a cell is: 
				- String
				- Mathematical Formula prepended with an "="
				- Numbers (doubles)
	
	- Formula creation: Prepending a "=" to a mathematical expression or valid cell name
			creates a formula that is automatically evaluated.
	
	- Error handling:  Error window is displayed explaining the nature of the error
			without crashing the application.  Can exit out of the window and try
			to fix the error, cells are unneffected by this process.  Implemented
			error catches are:
				- SpreadsheetReadWriteException, which handles all file save/write errors
				- Formula Error, which handles invalid but syntactically correct formulas
					i.e. = (1 + 50 / 0)
				- ArgumentException, which handles invalid arguments in a formula
					i.e. using a cell in a formula that holds a string value
				- FormulaFormatException, which handles invalid syntax in a formula
					i.e. to many operators or parenthesis
				- CircularException, which handles a circular reference in a formula
	
	- Help menu:  The Contents and About tabs in the Help dropdown hold information on
					the application.  The Contents tab holds information on how to use
					the spreadsheet.  The About tab holds information about the project,
					the assignment and the developers.

Additional Functionality:
	- 11/3 Display file name in the window title, default is "New".

	- 11/2 Ajustable spliter between cell value display and cell content editbox.

	- 11/1 Cell's default values are 0 when they are referenced from a formula,
		which results in less formula errors.
	
	- 11/1 Hotkeys for saving (Ctrl+S), opening (Ctrl+O), and New (Ctrl+N)





************************* PREVIOUS ASSIGNMENT INFORMATION *************************

Written by Benwei Shi for CS 3500 assignment, September 26 2016

This solution is branched from assignment 4 for assignment 5.
This solution will reuse the classes writen in assignment 2 and assignment 3. The 
classes are added as dll files named SpreadsheetUtilities.dll and Formula.dll.
The target framework of these dll files is .NET Framework 4.5.2


10/02/2016
I deside to remove the Cell type, because save the value of contents does not benefit very much.

old record from assignment 4:
09/29/2016
After submited assignment 2 and assignment 3, I have made a little change of them when
implementing assignment 4. Most of the change is about XML comment.
	1. DependencyGraph: Rewrited ReplaceDependees and ReplaceDependents methods, new implement 
		will not iterate all element to delete the old dependency.
	2. Formula: Rewited operator != and == to handle null argument.

Benwei Shi
09/29/2016
