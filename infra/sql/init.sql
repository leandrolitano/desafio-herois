-- Script opcional (SQL-first). O projeto atual usa EF Code-first.

IF DB_ID('DesafioHerois') IS NULL
BEGIN
  CREATE DATABASE DesafioHerois;
END
GO

USE DesafioHerois;
GO

IF OBJECT_ID('dbo.HeroisSuperpoderes') IS NOT NULL DROP TABLE dbo.HeroisSuperpoderes;
IF OBJECT_ID('dbo.Herois') IS NOT NULL DROP TABLE dbo.Herois;
IF OBJECT_ID('dbo.Superpoderes') IS NOT NULL DROP TABLE dbo.Superpoderes;
GO

CREATE TABLE dbo.Herois (
  Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
  Nome NVARCHAR(120) NOT NULL,
  NomeHeroi NVARCHAR(120) NOT NULL UNIQUE,
  DataNascimento DATETIME2 NOT NULL,
  Altura FLOAT NOT NULL,
  Peso FLOAT NOT NULL,
  RowVersion ROWVERSION NOT NULL
);

CREATE TABLE dbo.Superpoderes (
  Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
  Superpoder NVARCHAR(120) NOT NULL,
  Descricao NVARCHAR(400) NOT NULL
);

CREATE TABLE dbo.HeroisSuperpoderes (
  HeroiId INT NOT NULL,
  SuperpoderId INT NOT NULL,
  CONSTRAINT PK_HeroisSuperpoderes PRIMARY KEY (HeroiId, SuperpoderId),
  CONSTRAINT FK_HSP_Herois FOREIGN KEY (HeroiId) REFERENCES dbo.Herois(Id),
  CONSTRAINT FK_HSP_Superpoderes FOREIGN KEY (SuperpoderId) REFERENCES dbo.Superpoderes(Id)
);

INSERT INTO dbo.Superpoderes (Superpoder, Descricao)
VALUES
('Forca', 'Forca fisica acima do normal'),
('Velocidade', 'Movimenta-se muito rapido'),
('Voo', 'Capacidade de voar'),
('Invisibilidade', 'Pode ficar invisivel');
