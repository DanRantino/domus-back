# Segurança de dependências

O restore e o build auditam pacotes diretos e transitivos (`NuGetAudit` e `NuGetAuditMode=all` em `Directory.Build.props`). Advisory **high** (`NU1903`) e **critical** (`NU1904`) falham o restore e o build. Advisory **low** (`NU1901`) e **moderate** (`NU1902`) continuam aviso e não bloqueiam o merge.

O CI também executa `dotnet list Domus.sln package --vulnerable --include-transitive` depois do restore, para o log mostrar os pacotes afetados. A imagem de produção copia `Directory.Build.props` antes do restore e usa a mesma regra.

## Atualizar

Uma mudança de dependência entra por PR para `nonprod` e precisa passar no CI existente: restore, build e testes.

Não há upgrade automático de pacotes. Suba a versão no `PackageReference` de forma explícita. Se a única correção publicada for incompatível com o código atual, ela espera uma atualização deliberada, não um bump só para limpar o aviso.

## Automação futura

O próximo passo, ainda não habilitado, é o Dependabot do GitHub: PRs semanais de atualização contra `nonprod`, sem auto-merge. Cada PR continua sujeito ao mesmo CI. Não há scanner pago além da auditoria do SDK e dos alertas nativos do GitHub.
