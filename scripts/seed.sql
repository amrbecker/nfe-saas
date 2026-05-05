-- =============================================================
-- NfeSaas — Seed inicial
-- Empresa de demonstração + usuário admin
-- Senha padrão: Admin@123
-- =============================================================

-- Aguarda migrations (tabelas criadas pelo EF Core)
-- Este script é executado após o schema estar criado via migrations

DO $$
BEGIN

-- Inserir empresa de demonstração (se não existir)
IF NOT EXISTS (SELECT 1 FROM empresas WHERE "Cnpj" = '00000000000191') THEN
    INSERT INTO empresas (
        "Id", "CreatedAt", "IsDeleted",
        "RazaoSocial", "NomeFantasia", "Cnpj",
        "InscricaoEstadual", "Logradouro", "Numero",
        "Bairro", "Cidade", "Uf", "Cep",
        "CodigoMunicipio", "Telefone", "Email",
        "RegimeTributario", "AmbienteSefaz",
        "UltimoNumeronFe", "UltimoNumeronFCe",
        "SerieNFe", "SerieNFCe", "Ativo"
    ) VALUES (
        'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
        NOW(), false,
        'EMPRESA DEMONSTRAÇÃO LTDA',
        'EMPRESA DEMO',
        '00000000000191',
        '000000000',
        'Av. Paulista', '1000',
        'Bela Vista', 'São Paulo', 'SP', '01310100',
        '3550308', '11999999999', 'demo@empresa.com.br',
        3,  -- RegimeNormal
        2,  -- Homologacao
        0, 0, 1, 1, true
    );
    RAISE NOTICE 'Empresa de demonstração criada.';
END IF;

-- Inserir usuário admin (senha: Admin@123)
-- Hash BCrypt gerado para "Admin@123"
IF NOT EXISTS (SELECT 1 FROM usuarios WHERE "Email" = 'admin@nfesaas.com.br') THEN
    INSERT INTO usuarios (
        "Id", "CreatedAt", "IsDeleted",
        "EmpresaId", "Nome", "Email",
        "SenhaHash", "Role", "Ativo"
    ) VALUES (
        'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
        NOW(), false,
        'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
        'Administrador',
        'admin@nfesaas.com.br',
        '$2a$11$PsoQJHysEFRocLlyRdeuf.yoOQ2Q2/1rkWxszHnVnFF7FnCIoBml6',
        'Admin',
        true
    );
    RAISE NOTICE 'Usuário admin criado: admin@nfesaas.com.br / Admin@123';
END IF;

END $$;
