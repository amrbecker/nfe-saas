Feature: Gestão de Empresas e Usuários
  Como administrador do escritório
  Quero gerenciar as empresas e usuários do meu escritório
  Para manter o controle de acesso e cadastros

  Scenario: Administrador pode criar uma empresa no escritório
    Given existe um escritório com CNPJ "71234567000191" e admin "gestao_admin1@bdd.com" com senha "Gestao@123"
    And estou autenticado como "gestao_admin1@bdd.com" com senha "Gestao@123"
    When crio uma empresa com os dados:
      | Campo            | Valor              |
      | RazaoSocial      | Empresa BDD Ltda   |
      | NomeFantasia     | BDD Empresa        |
      | Cnpj             | 72234567000191     |
      | InscricaoEstadual| IE999              |
    Then a resposta deve ter status 200
    And a empresa aparece na listagem do escritório

  Scenario: Usuário comum não pode criar empresa
    Given existe um escritório com CNPJ "81234567000191" e admin "gestao_admin2@bdd.com" com senha "Gestao@123"
    And existe um usuário "gestao_user1@bdd.com" com senha "User@123" e role "User" no mesmo escritório com CNPJ "81234567000191"
    And estou autenticado como "gestao_user1@bdd.com" com senha "User@123"
    When tento criar uma empresa com CNPJ "82234567000191"
    Then a resposta deve ter status 403

  Scenario: Administrador pode criar um usuário no escritório
    Given existe um escritório com CNPJ "91234567000191" e admin "gestao_admin3@bdd.com" com senha "Gestao@123"
    And estou autenticado como "gestao_admin3@bdd.com" com senha "Gestao@123"
    When crio um usuário com nome "Novo Usuário" e email "novo_usuario@bdd.com" e senha "NovoUser@123"
    Then a resposta deve ter status 200
    And o usuário "novo_usuario@bdd.com" aparece na listagem do escritório
