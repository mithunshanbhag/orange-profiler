IF NOT "%VS90COMNTOOLS%" == "" (
"%VS90COMNTOOLS%vsvars32.bat"
resgen Form1.resx ShapePainter.Form1.resources
csc.exe /t:winexe /debug+ /out:.\ShapePainter.exe DrawingBoard.cs Form1.cs Shapes.cs ShapesCollection.cs /resource:ShapePainter.Form1.resources
del ShapePainter.Form1.resources
) ELSE (
IF NOT "%VS80COMNTOOLS%" == "" (
"%VS80COMNTOOLS%vsvars32.bat"
resgen Form1.resx ShapePainter.Form1.resources
csc.exe /t:winexe /debug+ /out:.\ShapePainter.exe DrawingBoard.cs Form1.cs Shapes.cs ShapesCollection.cs /resource:ShapePainter.Form1.resources
del ShapePainter.Form1.resources
) ELSE (
IF NOT "%VS71COMNTOOLS%" == "" (
"%VS71COMNTOOLS%vsvars32.bat"
del ShapePainter.pdb
resgen Form1.resx ShapePainter.Form1.resources
csc.exe /t:winexe /debug+ /out:.\ShapePainter.exe DrawingBoard.cs Form1.cs Shapes.cs ShapesCollection.cs /resource:ShapePainter.Form1.resources
del ShapePainter.Form1.resources
)
)
)


