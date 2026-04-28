using System.Threading;

namespace AlgoritmoBanqueiro;

/// <summary>
/// Simulação multithread do algoritmo do banqueiro (Silberschatz et al., cap. 7).
/// </summary>
internal static class Program
{
    private const int NumeroClientes = 5;

    private static int NumeroRecursos;
    private static int[] Disponivel = null!;
    private static int[,] Maximo = null!;
    private static int[,] Alocacao = null!;
    private static int[,] Necessidade = null!;

    private static readonly Mutex MutexBanqueiro = new();

    /// <summary>Equivale a request_resources: 0 = ok, -1 = negado.</summary>
    private static int SolicitarRecursos(int numeroCliente, int[] pedido)
    {
        if (pedido.Length != NumeroRecursos)
            return -1;

        MutexBanqueiro.WaitOne();
        try
        {
            for (int j = 0; j < NumeroRecursos; j++)
            {
                if (pedido[j] < 0 || pedido[j] > Necessidade[numeroCliente, j])
                    return -1;
            }

            for (int j = 0; j < NumeroRecursos; j++)
            {
                if (pedido[j] > Disponivel[j])
                    return -1;
            }

            for (int j = 0; j < NumeroRecursos; j++)
            {
                Disponivel[j] -= pedido[j];
                Alocacao[numeroCliente, j] += pedido[j];
                Necessidade[numeroCliente, j] -= pedido[j];
            }

            if (!EstadoSeguro())
            {
                for (int j = 0; j < NumeroRecursos; j++)
                {
                    Disponivel[j] += pedido[j];
                    Alocacao[numeroCliente, j] -= pedido[j];
                    Necessidade[numeroCliente, j] += pedido[j];
                }
                return -1;
            }

            return 0;
        }
        finally
        {
            MutexBanqueiro.ReleaseMutex();
        }
    }

    /// <summary>Equivale a release_resources: 0 = ok, -1 = erro.</summary>
    private static int LiberarRecursos(int numeroCliente, int[] liberacao)
    {
        if (liberacao.Length != NumeroRecursos)
            return -1;

        MutexBanqueiro.WaitOne();
        try
        {
            for (int j = 0; j < NumeroRecursos; j++)
            {
                if (liberacao[j] < 0 || liberacao[j] > Alocacao[numeroCliente, j])
                    return -1;
            }

            for (int j = 0; j < NumeroRecursos; j++)
            {
                Alocacao[numeroCliente, j] -= liberacao[j];
                Disponivel[j] += liberacao[j];
                Necessidade[numeroCliente, j] += liberacao[j];
            }

            return 0;
        }
        finally
        {
            MutexBanqueiro.ReleaseMutex();
        }
    }

    private static bool EstadoSeguro()
    {
        int[] trabalho = new int[NumeroRecursos];
        Array.Copy(Disponivel, trabalho, NumeroRecursos);

        bool[] terminou = new bool[NumeroClientes];
        bool progrediu;

        do
        {
            progrediu = false;
            for (int i = 0; i < NumeroClientes; i++)
            {
                if (terminou[i])
                    continue;

                bool cabe = true;
                for (int j = 0; j < NumeroRecursos; j++)
                {
                    if (Necessidade[i, j] > trabalho[j])
                    {
                        cabe = false;
                        break;
                    }
                }

                if (!cabe)
                    continue;

                for (int j = 0; j < NumeroRecursos; j++)
                    trabalho[j] += Alocacao[i, j];

                terminou[i] = true;
                progrediu = true;
            }
        } while (progrediu);

        for (int i = 0; i < NumeroClientes; i++)
        {
            if (!terminou[i])
                return false;
        }

        return true;
    }

    private static void InicializarMaximos()
    {
        var aleatorio = new Random();

        for (int j = 0; j < NumeroRecursos; j++)
        {
            int restante = Disponivel[j];
            for (int i = 0; i < NumeroClientes - 1; i++)
            {
                int teto = Math.Max(0, restante);
                int valor = teto == 0 ? 0 : aleatorio.Next(0, teto + 1);
                Maximo[i, j] = valor;
                restante -= valor;
            }
            Maximo[NumeroClientes - 1, j] = Math.Max(0, restante);
        }

        for (int i = 0; i < NumeroClientes; i++)
        {
            for (int j = 0; j < NumeroRecursos; j++)
            {
                Alocacao[i, j] = 0;
                Necessidade[i, j] = Maximo[i, j];
            }
        }
    }

    private static void RotinaCliente(object? idObj)
    {
        int idCliente = (int)idObj!;

        for (int ciclo = 0; ciclo < 30; ciclo++)
        {
            Thread.Sleep(Random.Shared.Next(30, 150));

            int[] pedido = new int[NumeroRecursos];
            MutexBanqueiro.WaitOne();
            try
            {
                for (int j = 0; j < NumeroRecursos; j++)
                {
                    int maxPedido = Necessidade[idCliente, j];
                    pedido[j] = Random.Shared.Next(0, maxPedido + 1);
                }
            }
            finally
            {
                MutexBanqueiro.ReleaseMutex();
            }

            // Em alguns ciclos, força um pedido inválido para gerar NEGADO no fluxo normal.
            if (Random.Shared.Next(0, 100) < 15)
            {
                int recurso = Random.Shared.Next(0, NumeroRecursos);
                pedido[recurso] = Necessidade[idCliente, recurso] + 1;
            }

            int resultado = SolicitarRecursos(idCliente, pedido);
            if (resultado == 0)
            {
                Console.WriteLine($"[Cliente {idCliente}] Pedido ACEITO: [{string.Join(", ", pedido)}]");
                Thread.Sleep(Random.Shared.Next(20, 80));

                int[] liberacao = new int[NumeroRecursos];
                MutexBanqueiro.WaitOne();
                try
                {
                    for (int j = 0; j < NumeroRecursos; j++)
                    {
                        int alocado = Alocacao[idCliente, j];
                        liberacao[j] = alocado == 0 ? 0 : Random.Shared.Next(0, alocado + 1);
                    }
                }
                finally
                {
                    MutexBanqueiro.ReleaseMutex();
                }

                if (LiberarRecursos(idCliente, liberacao) == 0)
                    Console.WriteLine($"[Cliente {idCliente}] Liberou: [{string.Join(", ", liberacao)}]");
            }
            else
            {
                Console.WriteLine($"[Cliente {idCliente}] Pedido NEGADO (inseguro ou inválido): [{string.Join(", ", pedido)}]");
            }
        }

        // Ao terminar, devolve o que ainda estava alocado (senão o vetor Disponivel não fecha com o total inicial)
        int[] restante = new int[NumeroRecursos];
        MutexBanqueiro.WaitOne();
        try
        {
            for (int j = 0; j < NumeroRecursos; j++)
                restante[j] = Alocacao[idCliente, j];
        }
        finally
        {
            MutexBanqueiro.ReleaseMutex();
        }

        LiberarRecursos(idCliente, restante);
    }

    private static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Uso: dotnet run -- <recurso1> <recurso2> ...");
            Console.WriteLine("Exemplo: dotnet run -- 10 5 7");
            return;
        }

        NumeroRecursos = args.Length;
        Disponivel = new int[NumeroRecursos];
        for (int j = 0; j < NumeroRecursos; j++)
        {
            if (!int.TryParse(args[j], out int v) || v < 0)
            {
                Console.WriteLine($"Argumento inválido: '{args[j]}' (use inteiros >= 0).");
                return;
            }
            Disponivel[j] = v;
        }

        Maximo = new int[NumeroClientes, NumeroRecursos];
        Alocacao = new int[NumeroClientes, NumeroRecursos];
        Necessidade = new int[NumeroClientes, NumeroRecursos];

        InicializarMaximos();

        Console.WriteLine("--- Algoritmo do Banqueiro ---");
        Console.WriteLine($"Recursos iniciais: [{string.Join(", ", Disponivel)}]");
        Console.WriteLine($"Clientes: {NumeroClientes}");
        Console.WriteLine();

        var threads = new Thread[NumeroClientes];
        for (int i = 0; i < NumeroClientes; i++)
        {
            threads[i] = new Thread(RotinaCliente);
            threads[i].Start(i);
        }

        foreach (var t in threads)
            t.Join();

        Console.WriteLine();
        Console.WriteLine("--- Fim da simulação ---");
        Console.WriteLine($"Recursos finais (soma deve ser igual à inicial): [{string.Join(", ", Disponivel)}]");
    }
}
