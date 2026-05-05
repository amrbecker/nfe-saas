Feature: Autenticação
  Como usuário do sistema NfeSaas
  Quero realizar login com minhas credenciais
  Para acessar as funcionalidades do sistema

  Scenario: Login bem-sucedido com credenciais corretas
    Given existe um escritório com CNPJ "01234567000191" e admin "bdd_admin1@teste.com" com senha "Bdd@123"
    When faço login com email "bdd_admin1@teste.com" e senha "Bdd@123"
    Then a resposta deve ter status 200
    And recebo um token de acesso válido
    And recebo a lista de empresas do escritório

  Scenario: Login com senha incorreta retorna 401
    Given existe um escritório com CNPJ "02234567000191" e admin "bdd_admin2@teste.com" com senha "Bdd@123"
    When faço login com email "bdd_admin2@teste.com" e senha "SenhaErrada999"
    Then a resposta deve ter status 401

  Scenario: Login com e-mail inexistente retorna 401
    When faço login com email "inexistente@bdd.com" e senha "Qualquer@123"
    Then a resposta deve ter status 401

  Scenario: Selecionar empresa do próprio escritório gera novo token
    Given existe um escritório com CNPJ "03234567000191" e admin "bdd_admin3@teste.com" com senha "Bdd@123"
    And o escritório possui uma empresa com CNPJ "04234567000191"
    And estou autenticado como "bdd_admin3@teste.com" com senha "Bdd@123"
    When seleciono a empresa com CNPJ "04234567000191"
    Then a resposta deve ter status 200
    And recebo um novo token com empresa selecionada
