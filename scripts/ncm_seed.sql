-- =================================================================================
-- NfeSaas — Seed MÍNIMO da tabela NCM (32 NCMs para dev/teste).
--
-- Para CARGA COMPLETA (~10.500 NCMs oficiais), use `load_ncm_oficial.py`:
--
--   1) Baixe o JSON oficial em https://portalunico.siscomex.gov.br/
--      (arquivo: Tabela_NCM_Vigente_<data>.json)
--   2) python scripts/load_ncm_oficial.py <caminho_para_json> <versao>
--   3) MSYS_NO_PATHCONV=1 docker cp scripts/ncm_full_seed.sql nfesaas_postgres:/tmp/
--   4) MSYS_NO_PATHCONV=1 docker exec nfesaas_postgres psql -U nfesaas -d nfesaas -f /tmp/ncm_full_seed.sql
--
-- Este `ncm_seed.sql` cobre 32 NCMs frequentes e é IDEMPOTENTE — útil
-- para ambientes de desenvolvimento sem acesso ao JSON oficial.
--
-- Pré-requisito: a tabela `ncms` já deve existir (migration EF 20260511143746_AddNcmsTable).
--
-- Aplicação:
--   MSYS_NO_PATHCONV=1 docker cp scripts/ncm_seed.sql nfesaas_postgres:/tmp/ncm_seed.sql
--   MSYS_NO_PATHCONV=1 docker exec nfesaas_postgres psql -U nfesaas -d nfesaas -f /tmp/ncm_seed.sql
-- =================================================================================

BEGIN;

-- Índice trigram para busca textual rápida na descrição (Postgres pg_trgm).
-- A migration EF cria apenas os índices btree; este complementa com GIN para ILIKE.
CREATE EXTENSION IF NOT EXISTS pg_trgm;
CREATE INDEX IF NOT EXISTS ix_ncms_descricao_trgm
    ON ncms USING gin ("Descricao" gin_trgm_ops);

-- =================================================================================
-- SEED: 32 NCMs frequentes (~6% das emissões cobrem ~80% dos casos comuns)
-- Fonte: Tabela NCM/SH 2024 — Receita Federal / MDIC
-- =================================================================================

INSERT INTO ncms ("Codigo", "Descricao", "CategoriaCapitulo", "Posicao",
                  "AliquotaIpiPadrao", "ExigeCest", "Ativo", "VersaoTabela", "AtualizadoEm")
VALUES
    -- Alimentos / bebidas
    ('22011000', 'Águas minerais e águas gaseificadas',                 '22', '2201', 0.00, TRUE, TRUE, '2024-12', NOW()),
    ('22021000', 'Refrigerantes e bebidas não alcoólicas',              '22', '2202', 5.00, TRUE, TRUE, '2024-12', NOW()),
    ('22030000', 'Cervejas de malte',                                   '22', '2203', 6.00, TRUE, TRUE, '2024-12', NOW()),
    ('19053100', 'Bolachas, biscoitos doces e wafers',                  '19', '1905', 0.00, FALSE, TRUE, '2024-12', NOW()),
    ('21069090', 'Outras preparações alimentícias não especificadas',   '21', '2106', 0.00, FALSE, TRUE, '2024-12', NOW()),

    -- Higiene / limpeza
    ('33051000', 'Xampus para os cabelos',                              '33', '3305', 0.00, TRUE, TRUE, '2024-12', NOW()),
    ('33072000', 'Desodorantes corporais e antiperspirantes',           '33', '3307', 0.00, TRUE, TRUE, '2024-12', NOW()),
    ('34022000', 'Detergentes e preparações para limpeza',              '34', '3402', 0.00, TRUE, TRUE, '2024-12', NOW()),

    -- Vestuário
    ('61091000', 'Camisetas e T-shirts de malha de algodão',            '61', '6109', 0.00, FALSE, TRUE, '2024-12', NOW()),
    ('62034200', 'Calças, jardineiras e bermudas de algodão',           '62', '6203', 0.00, FALSE, TRUE, '2024-12', NOW()),
    ('64041100', 'Calçados esportivos de matéria têxtil',               '64', '6404', 0.00, FALSE, TRUE, '2024-12', NOW()),

    -- Eletrônicos / informática
    ('85171231', 'Telefones celulares (smartphones)',                   '85', '8517', 0.00, TRUE, TRUE, '2024-12', NOW()),
    ('85176294', 'Modems e roteadores de banda larga',                  '85', '8517', 0.00, FALSE, TRUE, '2024-12', NOW()),
    ('84713012', 'Computadores portáteis (notebooks)',                  '84', '8471', 0.00, FALSE, TRUE, '2024-12', NOW()),
    ('84714900', 'Outras máquinas automáticas processamento de dados',  '84', '8471', 0.00, FALSE, TRUE, '2024-12', NOW()),
    ('85285200', 'Monitores LCD/LED',                                   '85', '8528', 0.00, FALSE, TRUE, '2024-12', NOW()),
    ('85287200', 'Aparelhos televisores em cores',                      '85', '8528', 0.00, TRUE, TRUE, '2024-12', NOW()),

    -- Móveis / construção
    ('94036000', 'Móveis de madeira',                                   '94', '9403', 0.00, FALSE, TRUE, '2024-12', NOW()),
    ('94051000', 'Lustres e outros aparelhos de iluminação elétrica',   '94', '9405', 0.00, FALSE, TRUE, '2024-12', NOW()),
    ('25232990', 'Cimento Portland comum',                              '25', '2523', 0.00, TRUE, TRUE, '2024-12', NOW()),

    -- Veículos / combustíveis
    ('87032310', 'Automóveis com motor 1500-3000 cc',                   '87', '8703', 25.00, TRUE, TRUE, '2024-12', NOW()),
    ('87114000', 'Motocicletas 500-800 cc',                             '87', '8711', 35.00, TRUE, TRUE, '2024-12', NOW()),
    ('27101259', 'Gasolina comum',                                      '27', '2710', 0.00, TRUE, TRUE, '2024-12', NOW()),
    ('27101921', 'Óleo diesel comum',                                   '27', '2710', 0.00, TRUE, TRUE, '2024-12', NOW()),

    -- Medicamentos
    ('30049099', 'Outros medicamentos para uso humano',                 '30', '3004', 0.00, TRUE, TRUE, '2024-12', NOW()),
    ('30021500', 'Vacinas para medicina humana',                        '30', '3002', 0.00, FALSE, TRUE, '2024-12', NOW()),

    -- Papel / livros
    ('48201000', 'Cadernos, livros de contabilidade e similares',       '48', '4820', 0.00, FALSE, TRUE, '2024-12', NOW()),
    ('49019900', 'Livros, brochuras e impressos similares',             '49', '4901', 0.00, FALSE, TRUE, '2024-12', NOW()),

    -- Brinquedos / lazer
    ('95030099', 'Outros brinquedos não especificados',                 '95', '9503', 0.00, FALSE, TRUE, '2024-12', NOW()),

    -- Ferramentas / mecânica
    ('82055900', 'Outras ferramentas manuais',                          '82', '8205', 0.00, FALSE, TRUE, '2024-12', NOW()),
    ('73181500', 'Outros parafusos e pinos roscados de ferro/aço',      '73', '7318', 0.00, FALSE, TRUE, '2024-12', NOW()),

    -- Item genérico (NFC-e)
    ('00000000', 'Item genérico (somente NFC-e para serviços)',         '00', '0000', 0.00, FALSE, TRUE, '2024-12', NOW())
ON CONFLICT ("Codigo") DO UPDATE SET
    "Descricao"           = EXCLUDED."Descricao",
    "CategoriaCapitulo"   = EXCLUDED."CategoriaCapitulo",
    "Posicao"             = EXCLUDED."Posicao",
    "AliquotaIpiPadrao"   = EXCLUDED."AliquotaIpiPadrao",
    "ExigeCest"           = EXCLUDED."ExigeCest",
    "VersaoTabela"        = EXCLUDED."VersaoTabela",
    "AtualizadoEm"        = NOW();

COMMIT;

-- =================================================================================
-- CARGA COMPLETA (~15.000 NCMs)
-- =================================================================================
--
-- Fonte oficial: Tabela NCM/SH publicada pelo MDIC/Camex.
--   https://www.gov.br/siscomex/pt-br/legislacao/nomenclatura-comum-do-mercosul-ncm
--   ou via Portal Único: tabelas em CSV/XLSX são publicadas periodicamente.
--
-- Etapas para carga completa:
--
-- 1) Baixar o arquivo CSV oficial (ex.: "Tabela_NCM_Vigente.csv") com 2 colunas:
--      Código (com pontos: 0101.10.00) ; Descrição
--
-- 2) Pré-processar para remover pontos e normalizar encoding:
--      iconv -f LATIN1 -t UTF-8 Tabela_NCM_Vigente.csv | \
--      awk -F';' 'NR>1 {gsub(/\./, "", $1); print $1"|"$2}' > /tmp/ncm_oficial.csv
--
-- 3) Copiar para o container:
--      MSYS_NO_PATHCONV=1 docker cp /tmp/ncm_oficial.csv nfesaas_postgres:/tmp/
--
-- 4) Criar tabela stage temporária:
--      CREATE TEMP TABLE ncms_stage (codigo TEXT, descricao TEXT);
--      \COPY ncms_stage FROM '/tmp/ncm_oficial.csv'
--           WITH (FORMAT csv, DELIMITER '|', ENCODING 'UTF8');
--
-- 5) Upsert para tabela definitiva preservando capítulo/posição:
--      INSERT INTO ncms ("Codigo", "Descricao", "CategoriaCapitulo", "Posicao",
--                        "ExigeCest", "Ativo", "VersaoTabela", "AtualizadoEm")
--      SELECT
--          regexp_replace(codigo, '[^0-9]', '', 'g'),
--          descricao,
--          substring(regexp_replace(codigo, '[^0-9]', '', 'g'), 1, 2),
--          substring(regexp_replace(codigo, '[^0-9]', '', 'g'), 1, 4),
--          FALSE,
--          TRUE,
--          '2024-12',
--          NOW()
--      FROM ncms_stage
--      WHERE length(regexp_replace(codigo, '[^0-9]', '', 'g')) = 8
--      ON CONFLICT ("Codigo") DO UPDATE SET
--          "Descricao"     = EXCLUDED."Descricao",
--          "VersaoTabela"  = EXCLUDED."VersaoTabela",
--          "AtualizadoEm"  = NOW();
--
-- O passo 5 normalmente carrega ~14.500 linhas em <2s no Postgres 16.
-- =================================================================================
