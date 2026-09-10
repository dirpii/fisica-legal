using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Liga os controles da HUD aos parametros da rampa e mostra as leituras.
public class HUDRampa : MonoBehaviour
{
    [Header("Referencias")]
    public Rampa rampa;
    public MovimentoCaixa movimento;

    [Header("Sliders de entrada")]
    public Slider sliderAngulo, sliderMuE, sliderMuC, sliderMassa, sliderForca, sliderV0;

    [Header("Rotulos dos valores")]
    public TMP_Text valorAngulo, valorMuE, valorMuC, valorMassa, valorForca, valorV0;

    [Header("Controles")]
    public Toggle toggleSentido;        // ligado = subindo a rampa
    public Button botaoSoltar, botaoLancar;

    [Header("Leituras")]
    public TMP_Text leituraEstado, leituraCriterio, leituraAceleracao,
                    leituraTermos, leituraVelocidade, leituraDistancia, leituraAviso;

    void Start()
    {
        Configurar(sliderAngulo, 0f, 45f, rampa.anguloGraus);
        Configurar(sliderMuE, 0f, 1.2f, rampa.muE);
        Configurar(sliderMuC, 0f, 1.2f, rampa.muC);
        Configurar(sliderMassa, 0.5f, 50f, rampa.massa);
        Configurar(sliderForca, 0f, 200f, rampa.forca);
        Configurar(sliderV0, 0f, 10f, 5f);

        toggleSentido.isOn = rampa.forcaRampaAcima;

        botaoSoltar.onClick.AddListener(movimento.Soltar);
        botaoLancar.onClick.AddListener(delegate { movimento.Lancar(sliderV0.value); });
    }

    static void Configurar(Slider s, float min, float max, float valor)
    {
        s.wholeNumbers = false;
        s.minValue = min; s.maxValue = max; s.value = valor;
    }

    void Update()
    {
        // mu_c nunca pode passar de mu_e
        bool corrigiu = sliderMuC.value > sliderMuE.value;
        if (corrigiu) sliderMuC.value = sliderMuE.value;

        rampa.anguloGraus = sliderAngulo.value;
        rampa.muE = sliderMuE.value;
        rampa.muC = sliderMuC.value;
        rampa.massa = sliderMassa.value;
        rampa.forca = sliderForca.value;
        rampa.forcaRampaAcima = toggleSentido.isOn;

        valorAngulo.text = rampa.anguloGraus.ToString("0.0") + " graus";
        valorMuE.text = rampa.muE.ToString("0.00");
        valorMuC.text = rampa.muC.ToString("0.00");
        valorMassa.text = rampa.massa.ToString("0.0") + " kg";
        valorForca.text = rampa.forca.ToString("0") + " N";
        valorV0.text = sliderV0.value.ToString("0.0") + " m/s";

        leituraAviso.text = corrigiu ? "mu_c foi limitado a mu_e" : "";

        // --- leituras de saida ---
        float th = rampa.AnguloEfetivo(movimento.s);
        float res = movimento.ResultanteParalelaSemAtrito();
        float teto = movimento.TetoDoAtritoEstatico();

        string estado = movimento.parada ? "PARADA"
                      : (movimento.velocidade > 0f ? "SUBINDO" : "DESCENDO");
        string trecho = movimento.s >= 0f ? "rampa" : "plano horizontal";
        leituraEstado.text = estado + "  (" + trecho + ")";

        string linha1 = "tan " + (th * Mathf.Rad2Deg).ToString("0.0")
                      + " = " + Mathf.Tan(th).ToString("0.000")
                      + "   |   mu_e = " + rampa.muE.ToString("0.000");
        string linha2 = "resultante paralela = " + res.ToString("0.00") + " N"
                      + "   |   teto do atrito = " + teto.ToString("0.00") + " N";
        leituraCriterio.text = linha1 + "\n" + linha2;

        leituraAceleracao.text = "a = " + movimento.aceleracao.ToString("0.000") + " m/s2";

        float tF = rampa.ForcaComSinal / rampa.massa;
        float tG = -Rampa.G * Mathf.Sin(th);
        float tA = 0f;
        if (!movimento.parada)
            tA = -movimento.SentidoDoDeslizamento()
               * rampa.muC * Rampa.G * Mathf.Cos(th);
        leituraTermos.text = "F/m = " + tF.ToString("+0.00;-0.00")
                           + "   gravidade = " + tG.ToString("+0.00;-0.00")
                           + "   atrito = " + tA.ToString("+0.00;-0.00");

        leituraVelocidade.text =
            "v = " + movimento.velocidade.ToString("0.000") + " m/s"
          + "   |   v na base = "
          + movimento.velocidadeNaBaseDaRampa.ToString("0.000") + " m/s";

        leituraDistancia.text = "distancia percorrida = "
          + movimento.distanciaPercorrida.ToString("0.000") + " m";
    }
}
