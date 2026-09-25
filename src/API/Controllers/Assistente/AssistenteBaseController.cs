using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NfeSaas.Application.Assistente;
using NfeSaas.Application.DTOs.Assistente;

namespace NfeSaas.API.Controllers;

/// <summary>Base de conhecimento da Ori: mapa para o motor de hipóteses da UI e artigo completo.</summary>
[Authorize]
[Route("api/assistente/base")]
public class AssistenteBaseController : BaseApiController
{
    private readonly IBaseConhecimento _base;
    public AssistenteBaseController(IBaseConhecimento baseConhecimento) => _base = baseConhecimento;

    [HttpGet("mapa")]
    public ActionResult<MapaKbDto> Mapa() =>
        Ok(new MapaKbDto(_base.Utilizaveis().Select(ParaResumo).ToList()));

    [HttpGet("artigos/{**id}")]
    public ActionResult<ArtigoDetalheDto> Artigo(string id)
    {
        var artigo = _base.Obter(id);
        var podeVerTodos = User.IsInRole(PapeisAssistente.Curador) || User.IsInRole(PapeisAssistente.Plataforma);
        if (artigo == null || (!podeVerTodos && !_base.Utilizaveis().Contains(artigo))) return NotFound();

        return Ok(new ArtigoDetalheDto(
            ParaResumo(artigo),
            artigo.Corpo,
            artigo.Fontes.Select(f => new FonteArtigoDto(f.Documento, f.Url, f.Nivel)).ToList(),
            artigo.Curador, artigo.VigenciaInicio, artigo.VigenciaFim, artigo.RevisarAte));
    }

    internal static ArtigoResumoDto ParaResumo(ArtigoKb a) => new(
        a.Id, a.Titulo, a.Categoria, a.NivelFonte, a.Status, a.ResumoCurto,
        a.CodigosRejeicao.ToList(), a.CamposRelacionados.ToList(), a.Telas.ToList(), a.VerificadoEm);
}
