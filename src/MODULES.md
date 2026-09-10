# Organização dos módulos

Dependências permitidas: Application -> Domain; Infrastructure -> Application; Api -> Application/Infrastructure. Domain não depende de EF, HTTP, cache ou UI.

Foundation contém identificação e verificações técnicas. Settings contém somente a entidade de configuração inicial. Para cada novo módulo, criar pastas com o mesmo nome nas camadas que ele utilizar; serviços dependem de interfaces de Application. Não criar dependências cíclicas entre módulos.

| Módulo reservado | Responsabilidade | Implementação comercial |
| --- | --- | --- |
| Identity | Usuários e autorização | Fase 1 |
| Establishments | Estabelecimentos | Fase 1 |
| Couriers | Entregadores, veículos, documentos | Fase 1 |
| Licensing | Licença semanal e pagamentos | Fase 4 |
| Training | Palestras e presença | Fase 4 |
| Deliveries | Solicitações, paradas, estados | Fases 2 e 4 |
| Dispatch | Ofertas e aceite | Fase 3 |
| Tracking | Presença e localização | Fase 3 |
| Proofs | Comprovantes e ocorrências | Fase 5 |
| Notifications | Push e eventos | Fase 3 |
| Settings | Configurações operacionais | Base na Fase 0, administração na Fase 4 |
| Auditing | Auditoria e relatórios | Eventos nas fases correspondentes; relatórios na Fase 5 |

Motivo: preservar o monólito modular sem antecipar entidades ou regras ainda não definidas. Registro: docs/mudancas/2026-09-09-02-fase-zero.md.
