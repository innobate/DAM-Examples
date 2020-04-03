CREATE TABLE [dbo].[Country] (
    [id]     INT            NOT NULL,
    [name]   NVARCHAR (100) NOT NULL,
    [alpha2] NVARCHAR (2)   NOT NULL,
    [alpha3] NVARCHAR (3)   NOT NULL,
    CONSTRAINT [PK_countries] PRIMARY KEY CLUSTERED ([id] ASC)
);

