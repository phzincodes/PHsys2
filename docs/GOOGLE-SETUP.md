# Google — configuração externa e limite atual

Infraestrutura implementada com Microsoft.AspNetCore.Authentication.Google 10.0.15, OAuth Authorization Code com PKCE, state/correlação do middleware e userinfo oficial HTTPS. Usa sub estável e exige email_verified. Não aceita tokens ID arbitrários enviados pelo cliente nem usa coincidência de email para vincular contas.

## Operador

1. No projeto Google autorizado do proprietário, prepare consentimento e um cliente OAuth do tipo aplicação web para o backend. A criação/configuração externa não foi realizada pelo Codex.
2. Redirect local exato: https://localhost:5178/signin-google. Produção: https://DOMINIO-PUBLICO/signin-google, com domínio/certificado próprios. Não usar localhost para usuários externos.
3. Segredos apenas no servidor: Authentication:Google:ClientId e ClientSecret. Compose aceita PHSYSTEM_GOOGLE_CLIENT_ID / PHSYSTEM_GOOGLE_CLIENT_SECRET no arquivo privado externo. Nunca colocar o segredo no desktop, JS, URL, Git ou conversar sobre seu valor.
4. Conferir AllowedHosts/proxy confiável, HTTPS, Data Protection persistente, banco e redirects. /api/v1/auth/providers mostra presença da configuração, não prova que o Google a aceitou.
5. Validar navegador real: sucesso, cancelamento, email não verificado, conta existente, expiração, MFA, suspensão e logout. Não houve esse teste externo nesta entrega.

## Desktop

Cliente gera verifier aleatório e challenge S256; broker guarda hash do segredo de polling e fluxo por cinco minutos, com limite128. O navegador padrão recebe só ID efêmero do fluxo. Credencial duradoura, poll token e verifier não são transmitidos pela URL. Consumo atômico exige ambas as provas e é de uso único. Cookie de aplicação é emitido ao cliente desktop, não ao navegador nesse modo. DPAPI continua opt-in. Nenhum inventário do PC é enviado.

O broker nativo vem desabilitado: Authentication:Google:EnableNativeBroker=false. Só habilitar após configurar e revisar/testar o fluxo real; Compose usa PHSYSTEM_GOOGLE_NATIVE_ENABLED. É memória por instância: reinício cancela fluxos e múltiplas instâncias precisam de afinidade ou um armazenamento compartilhado revisto. Não confundir com serviço de produção já validado.

Contas novas recebem nome único derivado do identificador do provedor, sem senha local e sem papel privilegiado. Cadastro registra aceite dos termos no fluxo e confirmação proveniente do provedor. Correspondência com email existente mostra orientação para o método original; vinculação explícita e remoção do último método ainda não foram implementadas. Contas com MFA e equipe são recusadas neste fluxo, exigindo o método original; não há bypass da política MFA.

Testes usam backchannel simulado isolado com Identity/SQLite e cookies reais de teste: PKCE, criação sem senha, email não verificado, colisão de email, cliente browser separado do cliente nativo, prova incorreta, replay/concorrência e expiração. Não simulam sucesso na aplicação de desenvolvimento sem credenciais reais.

Referências: [Google no ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/social/google-logins?view=aspnetcore-10.0), [GoogleOptions/PKCE](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.authentication.google.googleoptions?view=aspnetcore-10.0).
