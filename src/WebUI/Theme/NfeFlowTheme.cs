using MudBlazor;

namespace NfeSaas.WebUI.Theme;

/// <summary>
/// Sistema de design do NFeFlow — fonte única de verdade para tema claro/escuro, tipografia e
/// forma. Usado por <c>MainLayout</c> e <c>EmptyLayout</c> (antes cada layout tinha seu próprio
/// tema improvisado, e a tela de login sequer passava um tema, caindo no roxo default do
/// MudBlazor).
///
/// Paleta: "safira" profunda como cor de ação (mais distinta do azul Material genérico), navy
/// no header/menu (cria hierarquia — o header é mais escuro que os botões primários, recurso
/// comum em produtos fintech premium), latão/dourado como cor terciária usada com moderação
/// (badges e destaques, nunca cor de UI ampla). Tipografia: Newsreader (serif) só em títulos de
/// destaque (H1–H3), Public Sans no resto — legível em densidade alta (tabelas fiscais).
/// </summary>
public static class NfeFlowTheme
{
    // Espelhados em wwwroot/css/nfeflow-theme.css como custom properties, para os poucos lugares
    // que hoje usam Style="..." com hex cru em vez de Color="Color.Primary".
    public const string Sapphire = "#2454A0";
    public const string Navy = "#132A52";
    public const string Petrol = "#0F6E5E";
    public const string Brass = "#C08A2E";

    private static readonly string[] DisplayFont = { "Newsreader", "Georgia", "serif" };
    private static readonly string[] UiFont = { "Public Sans", "-apple-system", "Segoe UI", "Roboto", "sans-serif" };

    public static readonly MudTheme Instance = new()
    {
        Palette = new PaletteLight
        {
            Primary = Sapphire,
            PrimaryDarken = "#132A52",
            PrimaryLighten = "#5B8AD1",
            PrimaryContrastText = "#FFFFFF",

            Secondary = Petrol,
            SecondaryContrastText = "#FFFFFF",

            Tertiary = Brass,
            TertiaryContrastText = "#FFFFFF",

            Success = "#2E7D32",
            Warning = "#B45309",
            Error = "#B3261E",
            Info = Sapphire,

            // Background e Surface propositalmente iguais: o label flutuante do MudBlazor
            // (Variant.Outlined) pinta um retalho com --mud-palette-surface atrás de si para
            // "cortar" a borda — em qualquer formulário que não esteja dentro de um MudCard/
            // MudPaper explícito (vários neste app), esse retalho fica sobre --mud-palette-
            // background. Se forem cores diferentes, aparece como uma caixa clara destacada
            // atrás do label. Cards ainda se distinguem pela sombra (Elevation), não pelo tom.
            Background = "#F6F7FA",
            BackgroundGrey = "#EEF0F6",
            Surface = "#F6F7FA",

            AppbarBackground = Navy,
            AppbarText = "#F5F6FA",
            DrawerBackground = Navy,
            DrawerText = "#D7DEEE",
            DrawerIcon = "#AEB9D4",

            TextPrimary = "#141B2D",
            TextSecondary = "#5B6478",

            LinesDefault = "#E1E4EC",
            LinesInputs = "#C7CCDA",
            Divider = "#E1E4EC",

            ActionDefault = "#5B6478",
        },
        PaletteDark = new PaletteDark
        {
            Primary = "#5B8AD1",
            PrimaryContrastText = "#0A1120",

            Secondary = "#3FB39A",
            Tertiary = "#D9A94A",

            Success = "#4CAF50",
            Warning = "#D08A3E",
            Error = "#E2897E",
            Info = "#5B8AD1",

            // Ver comentário equivalente em PaletteLight — Background e Surface propositalmente
            // iguais, para o retalho do label flutuante nunca aparecer como uma caixa destacada
            // em formulários fora de um MudCard/MudPaper.
            Background = "#111A2C",
            BackgroundGrey = "#0A1120",
            Surface = "#111A2C",

            AppbarBackground = "#0A1120",
            AppbarText = "#E8ECF4",
            DrawerBackground = "#0F1830",
            DrawerText = "#C3CBE0",
            DrawerIcon = "#8993AE",

            TextPrimary = "#E8ECF4",
            TextSecondary = "#9AA5BD",

            LinesDefault = "#2A3450",
            LinesInputs = "#374468",
            Divider = "#2A3450",
        },
        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = "10px",
            AppbarHeight = "64px",
        },
        Typography = new Typography
        {
            Default = new Default { FontFamily = UiFont, FontSize = "0.875rem" },
            H1 = new H1 { FontFamily = DisplayFont, FontWeight = 600, FontSize = "2.6rem", LineHeight = 1.15 },
            H2 = new H2 { FontFamily = DisplayFont, FontWeight = 600, FontSize = "2.1rem", LineHeight = 1.2 },
            H3 = new H3 { FontFamily = DisplayFont, FontWeight = 500, FontSize = "1.7rem", LineHeight = 1.25 },
            H4 = new H4 { FontFamily = UiFont, FontWeight = 700, FontSize = "1.35rem" },
            H5 = new H5 { FontFamily = UiFont, FontWeight = 700, FontSize = "1.1rem" },
            H6 = new H6 { FontFamily = UiFont, FontWeight = 700, FontSize = "1rem" },
            Button = new Button { FontFamily = UiFont, FontWeight = 600, TextTransform = "none" },
            Body1 = new Body1 { FontFamily = UiFont },
            Body2 = new Body2 { FontFamily = UiFont },
            Subtitle1 = new Subtitle1 { FontFamily = UiFont },
            Subtitle2 = new Subtitle2 { FontFamily = UiFont },
            Caption = new Caption { FontFamily = UiFont },
            Overline = new Overline { FontFamily = UiFont, FontWeight = 700, LetterSpacing = ".06em" },
        },
    };
}
