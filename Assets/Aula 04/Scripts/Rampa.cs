using UnityEngine;

public class Rampa : MonoBehaviour
{
    public const float G = 9.8f; // Constante da aceleração da gravidade padrão (m/s²)

    [Header("Parametros ajustaveis em tempo de execucao")]
    [Range(0f, 45f)] public float anguloGraus = 30f;       // Ângulo de inclinação da rampa configurável via Inspector/UI
    [Range(0f, 1.2f)] public float muE = 0.35f;           // Coeficiente de atrito estático
    [Range(0f, 1.2f)] public float muC = 0.20f;           // Coeficiente de atrito cinético
    [Range(0.5f, 50f)] public float massa = 2f;           // Massa do objeto
    [Range(0f, 200f)] public float forca = 0f;            // Intensidade da força externa aplicada
    public bool forcaRampaAcima = true;                   // Direção da força externa (true = rampa acima, false = rampa abaixo)

    [Header("Geometria da rampa")]
    public float comprimentoRampa = 4f;
    public float comprimentoPlano = 10f;
    public float larguraPista = 2f;
    public float espessuraPista = 0.2f;
    public float ladoCaixa = 0.6f;

    [Header("Objetos da cena")]
    public Transform rampa;
    public Transform plano;
    public Transform caixa;

    // Propriedade que converte o ângulo de graus para radianos (necessário para funções trigonométricas da Unity)
    public float AnguloRad => anguloGraus * Mathf.Deg2Rad;

    // Retorna o valor numérico da força externa acompanhado de seu sinal de direção (+ ou -)
    public float ForcaComSinal => forcaRampaAcima ? forca : -forca;

    // Define qual ângulo theta usar dependendo de onde a caixa está:
    // Na rampa (s >= 0) usa o ângulo inclinado; no plano horizontal (s < 0) o ângulo é 0
    public float AnguloEfetivo(float s) => s >= 0f ? AnguloRad : 0f;

    // Retorna o versor tangente (direção do movimento) em função da posição "s"
    public Vector3 SentidoPositivo(float s)
    {
        if (s >= 0f)
        {
            float rad = AnguloRad;
            return new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f); // Direção inclinada subindo a rampa
        }
        else
        {
            return new Vector3(-1f, 0f, 0f); // Direção horizontal no plano
        }
    }

    // Retorna o vetor normal (perpendicular e apontando para fora da superfície de apoio)
    public Vector3 Normal(float s)
    {
        if (s >= 0f)
        {
            float rad = AnguloRad;
            return new Vector3(-Mathf.Sin(rad), Mathf.Cos(rad), 0f); // Perpendicular à rampa inclinada
        }
        else
        {
            return new Vector3(0f, 1f, 0f); // Perpendicular ao plano horizontal (apontando para cima)
        }
    }

    // Converte a posição escalar "s" em coordenadas espaciais reais de mundo para posicionar a caixa perfeitamente sobre a pista
    public Vector3 PosicaoDaCaixa(float s)
    {
        // Pega o centro atual da rampa e recua metade do comprimento para achar a base exata (s = 0)
        Vector3 centroRampa = rampa != null ? rampa.position : Vector3.zero;
        Vector3 baseRampa = centroRampa - new Vector3((comprimentoRampa / 2f) * Mathf.Cos(AnguloRad), (comprimentoRampa / 2f) * Mathf.Sin(AnguloRad), 0f);

        if (s >= 0f)
        {
            // Posiciona a caixa na rampa inclinada a partir da base real
            return baseRampa + new Vector3(s * Mathf.Cos(AnguloRad), s * Mathf.Sin(AnguloRad), 0f) + Normal(s) * (ladoCaixa * 0.5f);
        }
        else
        {
            // Posiciona a caixa no plano horizontal a partir da mesma base (s = 0), estendendo para a esquerda
            return baseRampa + new Vector3(s, 0f, 0f) + Vector3.up * (ladoCaixa * 0.5f);
        }
    }

    // Retorna a rotação visual da caixa de acordo com a inclinação da superfície onde ela se encontra
    public Quaternion RotacaoDaCaixa(float s)
    {
        if (s >= 0f)
        {
            return Quaternion.Euler(0f, 0f, anguloGraus); // Rotacionada com o ângulo da rampa
        }
        else
        {
            return Quaternion.identity; // Sem rotação (perfeitamente alinhada no plano)
        }
    }

    // Executado a cada frame para manter a geometria atualizada caso o usuário altere valores no Inspector
    void Update()
    {
        AtualizarGeometria();
    }

    // Atualiza fisicamente a inclinação, o posicionamento de ajuste de pivô e o tamanho visual dos objetos na cena
    public void AtualizarGeometria()
    {
        // 1. Configura a Rampa
        if (rampa != null)
        {
            rampa.localRotation = Quaternion.Euler(0f, 0f, anguloGraus);

            Vector3 escalaRampa = rampa.localScale;
            escalaRampa.x = comprimentoRampa;
            escalaRampa.y = espessuraPista;
            rampa.localScale = escalaRampa;

            // Desloca o centro para compensar o pivô central do cubo/quadrado da Unity
            float rad = AnguloRad;
            float offsetX = (comprimentoRampa / 2f) * Mathf.Cos(rad);
            float offsetY = (comprimentoRampa / 2f) * Mathf.Sin(rad);
            rampa.localPosition = new Vector3(offsetX, offsetY, 0f);
        }

        // 2. Configura o Plano Horizontal
        if (plano != null)
        {
            plano.localRotation = Quaternion.identity;

            Vector3 escalaPlano = plano.localScale;
            escalaPlano.x = comprimentoPlano;
            escalaPlano.y = espessuraPista;
            plano.localScale = escalaPlano;

            plano.localPosition = new Vector3(-comprimentoPlano / 2f, -espessuraPista / 2f, 0f);
        }

        // 3. Configura a Caixa
        if (caixa != null)
        {
            caixa.localScale = new Vector3(ladoCaixa, ladoCaixa, 1f);
        }
    }
}