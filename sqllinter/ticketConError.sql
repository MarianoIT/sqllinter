DECLARE @COUNT INT;
DECLARE @COUNT_USER INT = 0;

truncate table empresa;

SELECT * FROM Empresa where IdEmpresa = 1;

INSERT INTO Empresa 
	(
		 [NombreFantasia]
		,[FechaCreacion]
		,[NumeroCuit]
		,[RazonSocial]
	) 
VALUES ('Nombre', GETDATE(),NULL,NULL)

SET @ID_EMPRESA = @@IDENTITY;
SELECT * FROM Empresa where IdEmpresa = @ID_EMPRESA;		

UPDATE Empresa
SET Estado = 1;

INSERT INTO TiposEmpresa ([IdEmpresa] ,[IdTipoEmpresa]) VALUES (@ID_EMPRESA,q)	
	
DELETE FROM Empresa;

DROP TABLE Empresa;

