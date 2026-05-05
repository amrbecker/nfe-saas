CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260505011207_InitialCreate') THEN
    CREATE TABLE empresas (
        "Id" uuid NOT NULL,
        "RazaoSocial" character varying(150) NOT NULL,
        "NomeFantasia" character varying(150) NOT NULL,
        "Cnpj" character varying(14) NOT NULL,
        "InscricaoEstadual" character varying(20) NOT NULL,
        "InscricaoMunicipal" text,
        "Logradouro" text NOT NULL,
        "Numero" text NOT NULL,
        "Complemento" text,
        "Bairro" text NOT NULL,
        "Cidade" text NOT NULL,
        "Uf" text NOT NULL,
        "Cep" character varying(8) NOT NULL,
        "CodigoMunicipio" text NOT NULL,
        "Telefone" character varying(20) NOT NULL,
        "Email" character varying(100) NOT NULL,
        "RegimeTributario" integer NOT NULL,
        "AmbienteSefaz" integer NOT NULL,
        "UltimoNumeronFe" integer NOT NULL,
        "UltimoNumeronFCe" integer NOT NULL,
        "SerieNFe" integer NOT NULL,
        "SerieNFCe" integer NOT NULL,
        "CaminhoLogotipo" text,
        "Ativo" boolean NOT NULL,
        "CertificadoBytes" bytea,
        "CertificadoSenha" character varying(500),
        "CertificadoValidade" timestamp with time zone,
        "CertificadoCnpj" text,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone,
        "IsDeleted" boolean NOT NULL,
        CONSTRAINT "PK_empresas" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260505011207_InitialCreate') THEN
    CREATE TABLE notas_fiscais (
        "Id" uuid NOT NULL,
        "EmpresaId" uuid NOT NULL,
        "Tipo" integer NOT NULL,
        "Serie" integer NOT NULL,
        "Numero" integer NOT NULL,
        "ChaveAcesso" character varying(44),
        "Protocolo" character varying(50),
        "DataAutorizacao" timestamp with time zone,
        "Finalidade" integer NOT NULL,
        "TipoOperacao" integer NOT NULL,
        "Ambiente" integer NOT NULL,
        "Situacao" integer NOT NULL,
        "DestinatarioCpfCnpj" character varying(14),
        "DestinatarioRazaoSocial" character varying(150),
        "DestinatarioEmail" character varying(100),
        "DestinatarioLogradouro" text,
        "DestinatarioNumero" text,
        "DestinatarioComplemento" text,
        "DestinatarioBairro" text,
        "DestinatarioCidade" text,
        "DestinatarioUf" text,
        "DestinatarioCep" text,
        "DestinatarioCodigoMunicipio" text,
        "DestinatarioInscricaoEstadual" text,
        "DestinatarioTipoPessoa" integer NOT NULL,
        "TotalProdutos" numeric(15,2) NOT NULL,
        "TotalDesconto" numeric NOT NULL,
        "TotalIcms" numeric(15,2) NOT NULL,
        "TotalIcmsSt" numeric NOT NULL,
        "TotalPis" numeric NOT NULL,
        "TotalCofins" numeric NOT NULL,
        "TotalFrete" numeric NOT NULL,
        "TotalSeguro" numeric NOT NULL,
        "TotalOutrasDespesas" numeric NOT NULL,
        "TotalNota" numeric(15,2) NOT NULL,
        "ModalidadeFrete" integer NOT NULL,
        "TransportadoraCpfCnpj" text,
        "TransportadoraRazaoSocial" text,
        "FormaPagemento" text NOT NULL,
        "ValorPagamento" numeric NOT NULL,
        "XmlEnvio" text,
        "XmlRetorno" text,
        "XmlCancelamento" text,
        "MotivoRejeicao" character varying(500),
        "InformacoesAdicionais" character varying(2000),
        "DataEmissao" timestamp with time zone NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone,
        "IsDeleted" boolean NOT NULL,
        CONSTRAINT "PK_notas_fiscais" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_notas_fiscais_empresas_EmpresaId" FOREIGN KEY ("EmpresaId") REFERENCES empresas ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260505011207_InitialCreate') THEN
    CREATE TABLE usuarios (
        "Id" uuid NOT NULL,
        "EmpresaId" uuid NOT NULL,
        "Nome" character varying(100) NOT NULL,
        "Email" character varying(100) NOT NULL,
        "SenhaHash" character varying(500) NOT NULL,
        "Role" character varying(50) NOT NULL,
        "Ativo" boolean NOT NULL,
        "RefreshToken" character varying(500),
        "RefreshTokenExpiry" timestamp with time zone,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone,
        "IsDeleted" boolean NOT NULL,
        CONSTRAINT "PK_usuarios" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_usuarios_empresas_EmpresaId" FOREIGN KEY ("EmpresaId") REFERENCES empresas ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260505011207_InitialCreate') THEN
    CREATE TABLE itens_nota_fiscal (
        "Id" uuid NOT NULL,
        "NotaFiscalId" uuid NOT NULL,
        "NumeroItem" integer NOT NULL,
        "CodigoProduto" character varying(60) NOT NULL,
        "Descricao" character varying(120) NOT NULL,
        "CodigoEan" text,
        "Ncm" character varying(8) NOT NULL,
        "Cest" text,
        "Cfop" character varying(4) NOT NULL,
        "UnidadeComercial" character varying(6) NOT NULL,
        "Quantidade" numeric NOT NULL,
        "ValorUnitario" numeric(15,4) NOT NULL,
        "ValorTotal" numeric(15,2) NOT NULL,
        "ValorDesconto" numeric(15,2) NOT NULL,
        "OrigemMercadoria" integer NOT NULL,
        "CstIcms" integer NOT NULL,
        "BaseCalculoIcms" numeric(15,2) NOT NULL,
        "AliquotaIcms" numeric NOT NULL,
        "ValorIcms" numeric(15,2) NOT NULL,
        "BaseCalculoIcmsReducao" numeric,
        "ValorIcmsSt" numeric,
        "BaseCalculoIcmsSt" numeric,
        "AliquotaIcmsSt" numeric,
        "CstPis" integer NOT NULL,
        "BaseCalculoPis" numeric NOT NULL,
        "AliquotaPis" numeric NOT NULL,
        "ValorPis" numeric(15,2) NOT NULL,
        "CstCofins" integer NOT NULL,
        "BaseCalculoCofins" numeric NOT NULL,
        "AliquotaCofins" numeric NOT NULL,
        "ValorCofins" numeric(15,2) NOT NULL,
        "CstIpi" text,
        "AliquotaIpi" numeric,
        "ValorIpi" numeric,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone,
        "IsDeleted" boolean NOT NULL,
        CONSTRAINT "PK_itens_nota_fiscal" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_itens_nota_fiscal_notas_fiscais_NotaFiscalId" FOREIGN KEY ("NotaFiscalId") REFERENCES notas_fiscais ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260505011207_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_empresas_Cnpj" ON empresas ("Cnpj");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260505011207_InitialCreate') THEN
    CREATE INDEX "IX_itens_nota_fiscal_NotaFiscalId" ON itens_nota_fiscal ("NotaFiscalId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260505011207_InitialCreate') THEN
    CREATE INDEX "IX_notas_fiscais_ChaveAcesso" ON notas_fiscais ("ChaveAcesso");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260505011207_InitialCreate') THEN
    CREATE INDEX "IX_notas_fiscais_EmpresaId" ON notas_fiscais ("EmpresaId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260505011207_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_usuarios_Email" ON usuarios ("Email");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260505011207_InitialCreate') THEN
    CREATE INDEX "IX_usuarios_EmpresaId" ON usuarios ("EmpresaId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260505011207_InitialCreate') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260505011207_InitialCreate', '8.0.0');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260505041454_AddEscritorioMultiTenant') THEN
    ALTER TABLE usuarios DROP CONSTRAINT "FK_usuarios_empresas_EmpresaId";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260505041454_AddEscritorioMultiTenant') THEN
    ALTER TABLE usuarios RENAME COLUMN "EmpresaId" TO "EscritorioId";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260505041454_AddEscritorioMultiTenant') THEN
    ALTER INDEX "IX_usuarios_EmpresaId" RENAME TO "IX_usuarios_EscritorioId";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260505041454_AddEscritorioMultiTenant') THEN
    ALTER TABLE empresas ADD "EscritorioId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260505041454_AddEscritorioMultiTenant') THEN
    CREATE TABLE escritorios (
        "Id" uuid NOT NULL,
        "RazaoSocial" character varying(150) NOT NULL,
        "NomeFantasia" character varying(150) NOT NULL,
        "Cnpj" character varying(14) NOT NULL,
        "Email" character varying(100) NOT NULL,
        "Telefone" character varying(20),
        "Plano" integer NOT NULL,
        "Ativo" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone,
        "IsDeleted" boolean NOT NULL,
        CONSTRAINT "PK_escritorios" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260505041454_AddEscritorioMultiTenant') THEN
    CREATE INDEX "IX_empresas_EscritorioId" ON empresas ("EscritorioId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260505041454_AddEscritorioMultiTenant') THEN
    CREATE UNIQUE INDEX "IX_escritorios_Cnpj" ON escritorios ("Cnpj");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260505041454_AddEscritorioMultiTenant') THEN
    ALTER TABLE empresas ADD CONSTRAINT "FK_empresas_escritorios_EscritorioId" FOREIGN KEY ("EscritorioId") REFERENCES escritorios ("Id") ON DELETE CASCADE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260505041454_AddEscritorioMultiTenant') THEN
    ALTER TABLE usuarios ADD CONSTRAINT "FK_usuarios_escritorios_EscritorioId" FOREIGN KEY ("EscritorioId") REFERENCES escritorios ("Id") ON DELETE CASCADE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260505041454_AddEscritorioMultiTenant') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260505041454_AddEscritorioMultiTenant', '8.0.0');
    END IF;
END $EF$;
COMMIT;

