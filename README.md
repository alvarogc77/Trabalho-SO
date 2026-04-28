# Algoritmo do Banqueiro (C#)

Simulação multithread com mutex: vários clientes pedem e liberam recursos; o banqueiro só aceita pedidos que mantêm o sistema em estado seguro (algoritmo de segurança do livro Silberschatz et al.).

## Requisitos

- .NET SDK 8 ou superior (o projeto está em `net9.0`; ajuste no `.csproj` se precisar de versão anterior).  
  Baixe em: https://dotnet.microsoft.com/download

## Compilar

Na pasta do projeto (`AlgoritmoBanqueiro`):

```bash
dotnet build
```

## Executar

Passe na linha de comando a quantidade disponível de cada tipo de recurso (o número de argumentos define quantos tipos existem).

Exemplo do enunciado (3 tipos: 10, 5 e 7 instâncias):

```bash
dotnet run -- 10 5 7
```

No Windows (PowerShell), o `--` separa opções do `dotnet` dos argumentos do programa.

Se já tiver compilado:

```bash
dotnet run --no-build -- 10 5 7
```

## Observações

- `SolicitarRecursos` e `LiberarRecursos` no código equivalem a `request_resources` e `release_resources` do trabalho (retornam `0` ou `-1`).
- O acesso às estruturas compartilhadas usa um `Mutex` para evitar condição de corrida.
- A matriz de demanda máxima é inicializada de forma que a soma das demandas por coluna iguala o vetor inicial `Disponivel`, garantindo estado inicial seguro com alocação zero.
