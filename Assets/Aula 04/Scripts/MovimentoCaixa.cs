using UnityEngine;

// Requer obrigatoriamente que o GameObject possua o componente Rampa anexado para funcionar corretamente
[RequireComponent(typeof(Rampa))]
public class MovimentoCaixa : MonoBehaviour
{
    // Referência privada ao script que gerencia os parâmetros físicos e geométricos da rampa
    Rampa rampaRef;

    [Header("Estado (somente leitura)")]
    public float s;                     // Posição escalar ao longo do percurso (positivo = na rampa inclinada, negativo = no plano horizontal)
    public float velocidade;            // Velocidade escalar atual (positiva subindo/avançando, negativa descendo/recuando)
    public float aceleracao;            // Aceleração instantânea resultante calculada pelas Leis de Newton
    public bool parada = true;          // Flag de controle que indica se a caixa se encontra totalmente em repouso
    public float distanciaPercorrida;   // Acumulador da trajetória total percorrida pela caixa desde o início
    public float velocidadeNaBaseDaRampa; // Armazena a velocidade exata registrada no instante em que a caixa cruza a base (s = 0)

    // Executado no momento em que o script é carregado na cena (antes do Start)
    void Awake()
    {
        // Obtém e armazena a referência do componente Rampa localizado no mesmo GameObject
        rampaRef = GetComponent<Rampa>();
    }

    // Executado na inicialização do objeto no jogo
    void Start()
    {
        // Posiciona a caixa inicialmente no topo da rampa em estado de repouso
        Soltar();
    }

    // Método público para resetar e posicionar a caixa no topo máximo da rampa em repouso
    public void Soltar()
    {
        s = rampaRef.comprimentoRampa; // Define a posição escalar no limite superior da rampa
        velocidade = 0f;               // Zera a velocidade inicial
        parada = true;                 // Coloca a caixa em estado de repouso
        distanciaPercorrida = 0f;      // Reseta o contador de distância percorrida
        velocidadeNaBaseDaRampa = 0f;  // Reseta a velocidade registrada na base
        AtualizarPosicao();            // Aplica imediatamente a posição visual correspondente na cena
    }

    // Método público para disparar a caixa a partir da base com uma velocidade inicial informada
    public void Lancar(float v0)
    {
        s = 0f;                        // Posiciona a caixa exatamente na base (origem do percurso)
        velocidade = Mathf.Abs(v0);    // Atribui velocidade inicial positiva (direcionada para o sentido rampa acima)
        parada = false;                // Retira do estado de repouso para iniciar o movimento
        distanciaPercorrida = 0f;      // Reseta o contador de distância percorrida
        velocidadeNaBaseDaRampa = 0f;  // Reseta a velocidade registrada na base
        AtualizarPosicao();            // Aplica imediatamente a posição visual correspondente na cena
    }

    // Calcula a força resultante paralela à trajetória sem considerar os efeitos do atrito
    // Fórmula: F_resultante = Força Externa (com sinal) - Componente do Peso Paralela ao Plano (m * g * sin(theta))
    public float ResultanteParalelaSemAtrito()
    {
        float anguloTheta = rampaRef.AnguloEfetivo(s); // Obtém o ângulo efetivo (inclinado se na rampa, zero se no plano)
        return rampaRef.ForcaComSinal - rampaRef.massa * Rampa.G * Mathf.Sin(anguloTheta);
    }

    // Calcula o limite máximo suportado pelo atrito estático (o "teto" que mantém a caixa travada se as forças forem baixas)
    // Fórmula: Fat_max = mu_e * Força Normal, onde Normal = m * g * cos(theta)
    public float TetoDoAtritoEstatico()
    {
        float anguloTheta = rampaRef.AnguloEfetivo(s);
        float forcaNormal = rampaRef.massa * Rampa.G * Mathf.Cos(anguloTheta);
        return rampaRef.muE * forcaNormal;
    }

    // Verifica se as forças puxando a caixa conseguem superar o limite do atrito estático para iniciar o movimento
    // Retorna true se a intensidade da força resultante paralela for estritamente maior que o teto do atrito estático
    public bool DeveIniciarMovimento()
    {
        return Mathf.Abs(ResultanteParalelaSemAtrito()) > TetoDoAtritoEstatico();
    }

    // Determina o sentido do deslizamento (+1 para subindo/tendência para cima, -1 para descendo/tendência para baixo)
    // Necessário para orientar o atrito cinético de forma sempre contrária ao movimento real ou iminente
    public float SentidoDoDeslizamento()
    {
        if (!parada)
        {
            return Mathf.Sign(velocidade); // Se já está em movimento, utiliza o sinal da velocidade atual
        }
        else
        {
            float resultante = ResultanteParalelaSemAtrito();
            return resultante >= 0f ? 1f : -1f; // Se está parada, baseia-se na força resultante para prever o sentido de saída
        }
    }

    // Aplica a Segunda Lei de Newton projetada no eixo da pista para calcular a aceleração escalar instantânea:
    // a = (Força Externa / massa) - (g * sin(theta)) - (sentido * mu_c * g * cos(theta))
    public float CalcularAceleracao()
    {
        float anguloTheta = rampaRef.AnguloEfetivo(s);
        float termoForcaExterna = rampaRef.ForcaComSinal / rampaRef.massa;
        float termoGravidade = -Rampa.G * Mathf.Sin(anguloTheta);
        float termoAtrito = 0f;

        // O atrito cinético atua dissipando energia somente quando a caixa está em movimento efetivo
        if (!parada)
        {
            termoAtrito = -SentidoDoDeslizamento() * rampaRef.muC * Rampa.G * Mathf.Cos(anguloTheta);
        }

        return termoForcaExterna + termoGravidade + termoAtrito;
    }

    // Método de integração numérica executado a cada passo fixo de tempo da engine (ideal para cálculos físicos estáveis)
    void FixedUpdate()
    {
        float deltaTime = Time.fixedDeltaTime; // Obtém o intervalo de tempo fixo entre os quadros de física

        // Se a caixa estiver em repouso, avalia continuamente se as forças externas superaram o atrito para destravá-la
        if (parada)
        {
            if (DeveIniciarMovimento())
            {
                // Impede que o sistema tente destravar a caixa se ela já estiver prensada contra as bordas extremas da pista
                bool presoNoTopo = (s >= rampaRef.comprimentoRampa && ResultanteParalelaSemAtrito() > 0f);
                bool presoNoFundo = (s <= -rampaRef.comprimentoPlano && ResultanteParalelaSemAtrito() < 0f);

                if (!presoNoTopo && !presoNoFundo)
                {
                    parada = false; // Sai do estado de repouso e inicia o deslocamento físico
                }
            }
        }

        // Se a caixa estiver em movimento, processa a cinemática e a dinâmica passo a passo
        if (!parada)
        {
            aceleracao = CalcularAceleracao(); // Calcula a aceleração atual
            float sAntigo = s;                 // Armazena a posição anterior para detectar a passagem pela base (s = 0)
            float vAntigo = velocidade;        // Armazena a velocidade anterior para detectar inversão de sinal

            velocidade += aceleracao * deltaTime;       // Atualiza a velocidade escalar: v = v + a * dt

            // Se a velocidade tentou inverter o sinal (passou por zero), verifica se o atrito estático consegue segurá-la
            if (Mathf.Sign(vAntigo) != Mathf.Sign(velocidade) && vAntigo != 0f)
            {
                if (!DeveIniciarMovimento())
                {
                    velocidade = 0f;
                    parada = true; // Estaciona perfeitamente em vez de ficar oscilando
                }
            }

            if (!parada)
            {
                s += velocidade * deltaTime;                // Atualiza a posição escalar ao longo da pista: s = s + v * dt
                distanciaPercorrida += Mathf.Abs(velocidade * deltaTime); // Acumula o total da distância percorrida

                // Detecta o instante exato em que a caixa cruza a transição entre a rampa e o plano (ponto s = 0)
                if ((sAntigo > 0f && s <= 0f) || (sAntigo < 0f && s >= 0f))
                {
                    velocidadeNaBaseDaRampa = velocidade; // Registra a velocidade exata no momento de passagem pela base
                }

                // Trava rigorosa de limites nas extremidades para evitar que a caixa escape da pista ou da tela
                if (s <= -rampaRef.comprimentoPlano)
                {
                    s = -rampaRef.comprimentoPlano; // Trava a posição no limite esquerdo máximo do plano horizontal
                    velocidade = 0f;                // Zera a velocidade para estancar o movimento
                    parada = true;                  // Retorna ao estado de repouso
                }
                else if (s >= rampaRef.comprimentoRampa)
                {
                    s = rampaRef.comprimentoRampa;  // Trava a posição no limite superior máximo da rampa
                    velocidade = 0f;                // Zera a velocidade para absorver o impacto e evitar travamentos no topo
                    parada = true;                  // Retorna ao estado de repouso
                }
            }
        }
        else
        {
            aceleracao = 0f; // Garante aceleração nula se a caixa estiver totalmente parada
        }

        // Atualiza a representação visual da caixa na cena com base na nova posição escalar calculada
        AtualizarPosicao();
    }

    // Converte a posição escalar abstrata "s" em coordenadas reais no espaço 3D/2D do Unity e ajusta o Transform
    void AtualizarPosicao()
    {
        if (rampaRef.caixa != null)
        {
            rampaRef.caixa.position = rampaRef.PosicaoDaCaixa(s); // Atualiza a posição espacial considerando a geometria da pista
            rampaRef.caixa.rotation = rampaRef.RotacaoDaCaixa(s); // Atualiza a rotação visual (inclinada na rampa ou reta no plano)
        }
    }
}