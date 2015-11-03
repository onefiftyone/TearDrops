set reldir=.\PreReqs\
cd ..\..\..

copy "Simple.Data\Simple.Data\bin\%1\Simple.Data.dll" %reldir%
copy "Simple.Data\Simple.Data\bin\%1\Simple.Data.pdb" %reldir%
copy "Simple.Data\Simple.Data.Ado\bin\%1\Simple.Data.Ado.dll" %reldir%
copy "Simple.Data\Simple.Data.Ado\bin\%1\Simple.Data.Ado.pdb" %reldir%
copy "Simple.Data\Simple.Data.SqlServer\bin\%1\Simple.Data.SqlServer.dll" %reldir%
copy "Simple.Data\Simple.Data.SqlServer\bin\%1\Simple.Data.SqlServer.pdb" %reldir%