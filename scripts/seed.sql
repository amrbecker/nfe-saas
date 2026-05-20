-- =============================================================
-- NfeSaas — Seed multi-tenant
-- Escritório demo + Empresa demo + usuário admin
-- Senha padrão: Admin@123
-- =============================================================

DO $$
BEGIN

-- Inserir escritório de demonstração
IF NOT EXISTS (SELECT 1 FROM escritorios WHERE "Cnpj" = '99999999000191') THEN
    INSERT INTO escritorios (
        "Id", "CreatedAt", "IsDeleted",
        "RazaoSocial", "NomeFantasia", "Cnpj",
        "Email", "Telefone", "Plano", "Ativo",
        "TrialInicioEm", "TrialFimEm", "PlanoAtivoAteEm"
    ) VALUES (
        'cccccccc-cccc-cccc-cccc-cccccccccccc',
        NOW(), false,
        'ESCRITÓRIO CONTÁBIL DEMO LTDA',
        'CONTÁBIL DEMO',
        '99999999000191',
        'admin@escritoriodemo.com.br',
        '11999999900',
        1,  -- Basico
        true,
        NOW(),
        NOW() + INTERVAL '30 days',
        NOW() + INTERVAL '365 days'  -- demo: plano "pago" por 1 ano para não bloquear demonstrações
    );
    RAISE NOTICE 'Escritório de demonstração criado.';
END IF;

-- Inserir empresa de demonstração vinculada ao escritório
IF NOT EXISTS (SELECT 1 FROM empresas WHERE "Cnpj" = '00000000000191') THEN
    INSERT INTO empresas (
        "Id", "CreatedAt", "IsDeleted",
        "EscritorioId",
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
        'cccccccc-cccc-cccc-cccc-cccccccccccc',
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

-- Inserir usuário admin do escritório (senha: Admin@123)
IF NOT EXISTS (SELECT 1 FROM usuarios WHERE "Email" = 'admin@nfesaas.com.br') THEN
    INSERT INTO usuarios (
        "Id", "CreatedAt", "IsDeleted",
        "EscritorioId", "Nome", "Email",
        "SenhaHash", "Role", "Ativo"
    ) VALUES (
        'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
        NOW(), false,
        'cccccccc-cccc-cccc-cccc-cccccccccccc',
        'Administrador',
        'admin@nfesaas.com.br',
        '$2a$11$PsoQJHysEFRocLlyRdeuf.yoOQ2Q2/1rkWxszHnVnFF7FnCIoBml6',
        'Admin',
        true
    );
    RAISE NOTICE 'Usuário admin criado: admin@nfesaas.com.br / Admin@123';
END IF;

END $$;
