# 🚀 Liberdade Financeira (v1.0)

Sistema de Gestão de Evolução Patrimonial focado no atingimento da independência financeira através de estratégias de **alocação de ativos** e **rebalanceamento inteligente**.

O projeto foi desenhado para acompanhar o investidor desde a fase de **acumulação inicial** até a **geração de renda passiva** suficiente para cobrir os custos de vida.

---

## 🎯 Objetivo do Projeto

Gerenciar as três etapas fundamentais da liberdade financeira:

- **Acumulação**  
  Aportes mensais em Renda Fixa (Tesouro Selic) até cobrir o custo de moradia.

- **Transição**  
  Migração gradual para Renda Variável (FIIs de Papel e Tijolo) visando geração de fluxo de caixa isento.

- **Risco / Empreendedorismo**  
  Uso do excedente da renda passiva para novos empreendimentos.

---

## ✨ Funcionalidades Principais

- **Projeção Financeira**  
  Endpoint de inteligência que calcula, com base no aporte mensal e rendimentos, a data para atingir a liberdade financeira.

- **Gestão de Carteira**  
  Controle de ativos (Renda Fixa, FIIs, Ações) e transações (Aportes, Compras, Vendas, Proventos).

- **Notificações Motivacionais**  
  Integração com **ntfy.sh** para envio de alertas sobre progresso das metas e lembretes de investimento.

- **Background Jobs**  
  Workers responsáveis por atualizar dados de mercado e processar notificações em segundo plano.

---

## 🛠 Tech Stack

O projeto utiliza **.NET 9**, seguindo os princípios da **Clean Architecture** e **CQRS**:

- **Core:** .NET 9 (Minimal API)  
- **Arquitetura:** Clean Architecture + CQRS  
- **Banco de Dados:** SQLite (`liberdade.db`) com Entity Framework Core 9  
- **Mensageria Interna:** MediatR (Mediator Pattern)  
- **Consultas Dinâmicas:** Gridify  
- **Notificações:** Integração HTTP com ntfy.sh  

---

## 🏗 Estrutura da Arquitetura

A solução é modularizada para garantir separação de responsabilidades:

| Camada        | Responsabilidade |
|---------------|------------------|
| **Domain (Core)** | Entidades, regras de negócio, enums e constantes. Sem dependências externas. |
| **Application**  | Casos de uso (Commands/Queries), DTOs e interfaces. Onde reside o CQRS. |
| **Infrastructure** | Implementação de acesso a dados (EF Core), migrations e serviços externos. |
| **API** | Pontos de entrada (endpoints), configuração de DI e workers. |

---

## 🔌 API Endpoints (Visão Geral)

A API expõe funcionalidades RESTful para interação com front-end ou clientes HTTP.

### 🧠 Inteligência
- `GET /api/recomendacao`  
  Retorna sugestão de compra baseada no aporte disponível e no rebalanceamento da carteira.

- `GET /api/previsao`  
  Calcula a curva de patrimônio e estima a data de atingimento das metas de renda passiva.

### 💰 Transações
- `POST /api/transacoes/aporte`  
  Registro de entrada de capital (dinheiro novo).

- `POST /api/transacoes/compra`  
  Execução de ordens de compra de ativos.

### 📊 Ativos & Mercado
- `GET /api/ativos`  
  Listagem de ativos com dados de preço atual e performance.

- `POST /api/ativos`  
  Cadastro de novos ativos monitorados.

---

## ⚙️ Workers (Serviços em Segundo Plano)

O sistema possui serviços que rodam independentemente das requisições HTTP:

- **AtualizarMercadoWorker**  
  Mantém cotações, taxas (Selic) e rendimentos atualizados periodicamente.

- **NotificacaoInvestimentosWorker**  
  Monitora metas e envia insights motivacionais ao usuário.

---

## 🚀 Como Executar

1. Certifique-se de ter o **.NET SDK 9** instalado.  
2. Clone o repositório.  
3. Rode a aplicação:
```bash
dotnet run --project src/API
```
4. Acesse o Swagger em:
https://localhost:8080/swagger 
