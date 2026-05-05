Feature: Isolamento Multi-Tenant
  Como administrador do sistema
  Quero garantir que dados de um escritório não sejam acessíveis por outro
  Para manter a segurança e privacidade das informações

  Scenario: Usuário não pode selecionar empresa de outro escritório
    Given existe um escritório "Escritório Alfa" com CNPJ "11234567000191" e admin "alfa@bdd.com" com senha "Alfa@123" e empresa com CNPJ "12234567000191"
    And existe um escritório "Escritório Beta" com CNPJ "21234567000191" e admin "beta@bdd.com" com senha "Beta@123" e empresa com CNPJ "22234567000191"
    And estou autenticado como "alfa@bdd.com" com senha "Alfa@123"
    When seleciono a empresa do escritório "Escritório Beta"
    Then a resposta deve ter status 400

  Scenario: Listagem de empresas retorna apenas as do próprio escritório
    Given existe um escritório "Escritório Gama" com CNPJ "31234567000191" e admin "gama@bdd.com" com senha "Gama@123" e empresa com CNPJ "32234567000191"
    And existe um escritório "Escritório Delta" com CNPJ "41234567000191" e admin "delta@bdd.com" com senha "Delta@123" e empresa com CNPJ "42234567000191"
    And estou autenticado como "gama@bdd.com" com senha "Gama@123"
    When listo as empresas do escritório
    Then recebo apenas empresas do próprio escritório

  Scenario: Listagem de usuários retorna apenas os do próprio escritório
    Given existe um escritório "Escritório Épsilon" com CNPJ "51234567000191" e admin "epsilon@bdd.com" com senha "Epsilon@123" e empresa com CNPJ "52234567000191"
    And existe um escritório "Escritório Zeta" com CNPJ "61234567000191" e admin "zeta@bdd.com" com senha "Zeta@123" e empresa com CNPJ "62234567000191"
    And estou autenticado como "epsilon@bdd.com" com senha "Epsilon@123"
    When listo os usuários do escritório
    Then recebo apenas usuários do próprio escritório
