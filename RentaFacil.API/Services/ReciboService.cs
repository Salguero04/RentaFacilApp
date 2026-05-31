using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using RentaFacil.API.Models;
using RentaFacil.API.Repositories.Interfaces;

namespace RentaFacil.API.Services.Interfaces
{
    public interface IReciboService
    {
        Task<byte[]> GenerarReciboPdfAsync(int pagoId, string formato = "carta");
    }
}

namespace RentaFacil.API.Services
{
    public class ReciboService : Interfaces.IReciboService
    {
        private readonly IPagoRepository _pagoRepository;
        private readonly IContratoRepository _contratoRepository;
        private readonly IInquilinoRepository _inquilinoRepository;

        public ReciboService(IPagoRepository pagoRepository, IContratoRepository contratoRepository, IInquilinoRepository inquilinoRepository)
        {
            _pagoRepository = pagoRepository;
            _contratoRepository = contratoRepository;
            _inquilinoRepository = inquilinoRepository;
        }

        public async Task<byte[]> GenerarReciboPdfAsync(int pagoId, string formato = "carta")
        {
            var pago = await _pagoRepository.GetByIdAsync(pagoId);
            if (pago == null) throw new Exception("Pago no encontrado");

            var contrato = await _contratoRepository.GetByIdAsync(pago.ContratoId);
            if (contrato == null) throw new Exception("Contrato no encontrado");

            var inquilino = await _inquilinoRepository.GetByIdAsync(contrato.InquilinoId);
            if (inquilino == null) throw new Exception("Inquilino no encontrado");

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    if (formato.ToLower() == "ticket")
                    {
                        page.Size(80, 200, Unit.Millimetre); // Ticket 80mm ancho, largo dinámico
                        page.Margin(5, Unit.Millimetre);
                    }
                    else
                    {
                        page.Size(PageSizes.A4); // Carta/A4
                        page.Margin(1, Unit.Centimetre);
                    }

                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(formato.ToLower() == "ticket" ? 10 : 12));

                    page.Header().Element(compose => ComposeHeader(compose, pago, inquilino, formato));
                    page.Content().Element(compose => ComposeContent(compose, pago, formato));
                    
                    if (formato.ToLower() != "ticket")
                    {
                        page.Footer().AlignCenter().Text(x =>
                        {
                            x.Span("Generado por RentaFácilApp - Página ");
                            x.CurrentPageNumber();
                        });
                    }
                });
            });

            return document.GeneratePdf();
        }

        private void ComposeHeader(IContainer container, Pago pago, Inquilino inquilino, string formato)
        {
            container.Row(row =>
            {
                row.RelativeItem().Column(column =>
                {
                    column.Item().AlignCenter().Text(formato == "ticket" ? "RECIBO DE ARRENDAMIENTO" : $"Recibo de Pago #{pago.Id}")
                        .FontSize(formato == "ticket" ? 14 : 20).SemiBold().FontColor(Colors.Blue.Darken2);
                    column.Item().Text(text =>
                    {
                        text.Span("Fecha: ").SemiBold();
                        text.Span($"{pago.FechaPago:dd/MM/yyyy}");
                    });
                    column.Item().Text(text =>
                    {
                        text.Span("Inquilino: ").SemiBold();
                        text.Span(inquilino.NombreCompleto);
                    });
                    column.Item().Text(text =>
                    {
                        text.Span("Identificación: ").SemiBold();
                        text.Span(inquilino.Identificacion);
                    });
                });
            });
        }

        private void ComposeContent(IContainer container, Pago pago, string formato)
        {
            container.PaddingVertical(1, Unit.Centimetre).Column(column =>
            {
                column.Spacing(5);
                column.Item().Text("Detalles del Pago").FontSize(14).SemiBold().Underline();
                
                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn();
                        columns.ConstantColumn(100);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Text("Concepto").SemiBold();
                        header.Cell().AlignRight().Text("Monto").SemiBold();
                        
                        header.Cell().ColumnSpan(2).PaddingTop(2).BorderBottom(1).BorderColor(Colors.Black);
                    });

                    table.Cell().Text($"Renta Periodo {pago.Periodo}");
                    table.Cell().AlignRight().Text($"${pago.TotalMonto}");

                    table.Cell().Text("Servicios Extra");
                    table.Cell().AlignRight().Text($"${pago.Servicios}");

                    table.Cell().PaddingTop(10).Text("Monto Recibido (A Cuenta)").SemiBold();
                    table.Cell().PaddingTop(10).AlignRight().Text($"${pago.ACuenta}").SemiBold();

                    table.Cell().Text("Restante");
                    var restante = (pago.TotalMonto + pago.Servicios) - pago.ACuenta;
                    table.Cell().AlignRight().Text($"${(restante > 0 ? restante : 0)}");
                });

                if (formato == "ticket")
                {
                    column.Item().PaddingTop(20).AlignCenter().Text("_________________________");
                    column.Item().AlignCenter().Text("Firma de quien recibe");
                }
                else
                {
                    if (pago.Completado)
                    {
                        column.Item().PaddingTop(20).AlignCenter().Text("PAGO COMPLETADO").FontSize(16).FontColor(Colors.Green.Darken2).SemiBold();
                    }
                    else
                    {
                        column.Item().PaddingTop(20).AlignCenter().Text("PAGO PARCIAL - PENDIENTE").FontSize(16).FontColor(Colors.Red.Darken2).SemiBold();
                    }
                    column.Item().PaddingTop(40).AlignCenter().Text("_________________________");
                    column.Item().AlignCenter().Text("Firma del Arrendador");
                }
            });
        }
    }
}
