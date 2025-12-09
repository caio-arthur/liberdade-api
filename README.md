# PROJETO: SISTEMA DE GESTÃO FINANCEIRA (LIBERDADE FINANCEIRA)

**Data do Snapshot:** 09/12/2025  
**Estado Atual:** Camada de Domínio e Infraestrutura configuradas. Migração inicial criada.

---

## 1. Visão Geral e Regras de Negócio

O sistema visa gerenciar a evolução patrimonial pessoal, estruturada em **três etapas**:

### Etapas do Processo

1. **Etapa 1 – Acumulação**  
   - Aportes mensais (~R$ 1.500)  
   - Saldo inicial em **Renda Fixa (Tesouro Selic 2031)**  
   - Objetivo: rendimentos cobrirem o aluguel  
   - **Meta:** R$ 600/mês  

2. **Etapa 2 – Transição / Renda**  
   - Migração gradual para **FIIs** (Papel e Tijolo)  
   - Geração de renda mensal isenta  

3. **Etapa 3 – Risco**  
   - Uso da renda excedente para **empreendedorismo**

### Requisito-chave

> O sistema deve possuir **Rebalanceamento Inteligente**, sugerindo quando **vender Selic** e **comprar FIIs** com base na **MetaAlocacao** da fase atual.

---

## 2. Stack Tecnológica

- **Framework:** .NET 9 (Minimal API)
- **Banco de Dados:** SQLite (`liberdade.db`)
- **ORM:** Entity Framework Core
- **Arquitetura:** Clean Architecture  

Core <- Infrastructure <- API / Worker


---

## 3. Estrutura de Código Atual (Domain & Infrastructure)

### 3.1. Entidades (Core/Entities)

> Todas as entidades estão nomeadas em **Português**.

#### Ativo.cs

```csharp
public class Ativo
{
  public Guid Id { get; set; } = Guid.NewGuid();
  public string Codigo { get; set; } // Ex: "SELIC2031"
  public string Nome { get; set; }
  public AtivoCategoria Categoria { get; set; }
  public decimal PrecoAtual { get; set; }
  public decimal RendimentoValorMesAnterior { get; set; }
  public decimal PercentualDeRetornoMensalEsperado { get; set; } // Para projeções
  public DateTime AtualizadoEm { get; set; }
}
```


#### Transacao.cs
```csharp
public class Transacao
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? AtivoId { get; set; } // Nullable para permitir Aportes (Depósitos)
    public Ativo? Ativo { get; set; }
    public TransacaoTipo TipoTransacao { get; set; }
    public decimal Quantidade { get; set; }
    public decimal PrecoUnitario { get; set; }
    public decimal ValorTotal { get; set; }
    public DateTime Data { get; set; }
    public string Observacoes { get; set; }
}
```

#### MetaAlocacao.cs
(Define a estratégia da carteira)
```csharp
public class MetaAlocacao
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public AtivoCategoria Categoria { get; set; }
    public decimal PercentualAlvo { get; set; }
    public int NumeroFase { get; set; }
    public bool Ativa { get; set; }
}

```

#### PosicaoCarteira.cs
(Snapshot / View da carteira)
```csharp
public class PosicaoCarteira
{
    public Guid AtivoId { get; set; } // PK
    public string Codigo { get; set; }
    public AtivoCategoria Categoria { get; set; }
    public decimal Quantidade { get; set; }
    public decimal PrecoMedio { get; set; }
    public decimal PrecoAtual { get; set; }

    // Propriedade calculada (ignorada no banco)
    public decimal ValorTotalAtual => Quantidade * PrecoAtual;
}

```

### 3.2. Enums (Core/Enums)
AtivoCategoria:
- RendaFixaLiquidez
- FiiPapel
- FiiTijoloLogistica
- FiiTijoloShopping
- FiiHibrido
- Acoes

TransacaoTipo:
- Compra
- Venda
- RecebimentoProventos
- Aporte
- Resgate

### 3.3. Configurações do EF Core

Regras aplicadas para resolver limitações do SQLite e atender à lógica de negócio:

Enums
- Todos convertidos com .HasConversion<string>().

Transacao
- AtivoId configurado como opcional (IsRequired(false)) para permitir Aportes de caixa (transações sem vínculo a um ativo).

PosicaoCarteira
- AtivoId definido como Primary Key.
- ValorTotalAtual configurado com builder.Ignore(...) pois é uma propriedade calculada em memória e não deve ser persistida.

Exemplo de configuração (pseudo-code):

```csharp
public class PosicaoCarteiraConfiguration : IEntityTypeConfiguration<PosicaoCarteira>
{
    public void Configure(EntityTypeBuilder<PosicaoCarteira> builder)
    {
        builder.HasKey(p => p.AtivoId);

        builder.Property(p => p.Categoria)
                   .HasConversion<string>();

        builder.Ignore(x => x.ValorTotalAtual);
    }
}
```

---
## 4. Status do Banco de Dados

- Migration `InitialCreate` criada e validada.
- Migrações criadas a partir do comando: dotnet ef migrations add "InitialCreate" -p src/Infrastructure -s src/API -o Migrations
- Banco de dados atualizado sempre que a aplicação é executada: `Migrate()` em `Program.cs`

---
## 5. Próximos Passos (Roadmap de Implementação)

**Imediato (Próxima Sessão)**

- Implementar Services

    - `AtivoService`  
        CRUD básico para cadastrar Tesouro Selic e FIIs.

    - `TransacaoService`  
        Lógica para processar:
        - Aporte (entrada de dinheiro — atualiza caixa).
        - Compra (uso de caixa para adquirir ativo).
        - Deve atualizar a tabela `PosicaoCarteira` ao processar uma transação.

- Endpoints (API)
    - `POST /api/transacoes/aporte` — Registrar entrada de dinheiro.
    - `POST /api/transacoes/compra` — Registrar compra de ativo.
    - `GET /api/carteira` — Retornar a posição atual consolidada.

**Futuro Próximo**

- Implementar Job (`Worker`) para atualizar cotações do Selic e FIIs via API externa.
- Implementar lógica de Juros Compostos para o endpoint de Previsão.

