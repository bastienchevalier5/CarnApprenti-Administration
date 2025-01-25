using iText.Kernel.Font;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.IO.Font.Constants;
using iText.Kernel.Colors;
using iText.Kernel.Geom;
using System.IO;
using System.Threading.Tasks;
using System.Diagnostics;
using iText.Layout.Properties;
using TextAlignment = iText.Layout.Properties.TextAlignment;
using iText.Layout.Borders;
using VerticalAlignment = iText.Layout.Properties.VerticalAlignment;
using Border = iText.Layout.Borders.Border;
using Cell = iText.Layout.Element.Cell;
using Microsoft.EntityFrameworkCore;
using static CarnApprenti.LivretApprentissageContext;
using iText.Kernel.Utils;
using Microsoft.Maui.ApplicationModel;
using Moq;
using Mysqlx.Prepare;
using System.Diagnostics.Metrics;
using System.Runtime.Intrinsics.X86;
using System;
using System.Transactions;
using System.Numerics;
using CarnApprenti.Components.Pages;
using iText.Kernel.Exceptions;
using Microsoft.Extensions.Logging;
using Path = System.IO.Path;

namespace CarnApprenti
{
    public class ModeleService
    {
        private readonly LivretApprentissageContext _context;
        private readonly ILogger<DatabaseService> _logger;

        private PdfFont _defaultFont;
        private PdfFont _boldFont;
        private PdfFont _italicFont;

        public ModeleService(LivretApprentissageContext context, ILogger<DatabaseService> logger)
        {
            _logger = logger;
            _context = context;
            InitializeFonts();
        }

        private void InitializeFonts()
        {
            try
            {
                _defaultFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
                _boldFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
                _italicFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_OBLIQUE);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Erreur d'initialisation des polices : {ex.Message}");
                throw new InvalidOperationException("Impossible d'initialiser les polices PDF", ex);
            }
        }

        public async Task<byte[]> GeneratePdfAsync(ulong idModele)
        {
            try
            {
                var modele = await _context.Modeles.FindAsync(idModele);


                if (modele == null)
                {
                    throw new InvalidOperationException($"Le modèle avec l'ID {idModele} n'a pas été trouvé.");
                }

                using var memoryStream = new MemoryStream();
                var writerProperties = new WriterProperties().SetPdfVersion(PdfVersion.PDF_2_0);
                using var writer = new PdfWriter(memoryStream, writerProperties);
                using var pdf = new PdfDocument(writer);
                using var document = new Document(pdf, PageSize.A4);

                document.SetMargins(50, 50, 50, 50);

                AddFirstPage(document, modele);
                await AddCompositionPagesAsync(document, modele);
                await AddPersonnelPage(document, modele);
                await AddEquipePedagogiquePage(document, modele);
                AddCompteRenduPageAsync(document, modele);
                AddObservationsPage(document, modele);

                AddFooter(document, modele);

                document.Close();
                return memoryStream.ToArray();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Critical PDF generation error: {ex.Message}");
                throw new InvalidOperationException("PDF generation failed", ex);
            }
        }

        private void AddFirstPage(Document document, Modele modele)
        {
            try
            {
                var title = new Paragraph("Livret d'apprentissage")
                    .SetFont(_boldFont)
                    .SetFontColor(new DeviceRgb(60, 63, 235))
                    .SetFontSize(40)
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetMarginBottom(20)
                    .SetMarginTop(178);
                document.Add(title);

                var groupName = new Paragraph(modele.Groupe?.Nom ?? "Groupe Inconnu")
                    .SetFont(_boldFont)
                    .SetFontColor(new DeviceRgb(60, 63, 235))
                    .SetFontSize(25)
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetMarginTop(0);
                document.Add(groupName);

                var nomParagraph = new Paragraph("Nom :")
                    .SetFont(_boldFont)
                    .SetFontSize(14)
                    .SetTextAlignment(TextAlignment.LEFT)
                    .SetMarginTop(65)
                    .SetMarginLeft(-20);
                document.Add(nomParagraph);

                var prenomParagraph = new Paragraph("Prénom :")
                    .SetFont(_boldFont)
                    .SetFontSize(14)
                    .SetTextAlignment(TextAlignment.LEFT)
                    .SetMarginTop(5)
                    .SetMarginLeft(-20);
                document.Add(prenomParagraph);

                var siteParagraph = new Paragraph($"Site : {modele.Site?.Nom ?? "Site Inconnu"}")
                    .SetFont(_boldFont)
                    .SetFontSize(14)
                    .SetTextAlignment(TextAlignment.LEFT)
                    .SetMarginTop(5)
                    .SetMarginLeft(-20);
                document.Add(siteParagraph);

                AddFooter(document, modele);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Erreur lors de l'ajout de la première page : {ex.Message}");
                throw;
            }
        }

        // Reste des méthodes inchangées sauf pour l'ajout des paramètres de groupe et site où nécessaire


        private async Task AddCompositionPagesAsync(Document document, Modele modele)
        {
            try
            {
                var compositions = await _context.Compositions
                    .Where(c => c.ModeleId == modele.Id)
                    .ToListAsync();

                if (compositions.Any())
                {
                    foreach (var composition in compositions)
                    {
                        var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                        var projectRoot = Path.Combine(baseDirectory, "..", "..", "..", "..", "..", "wwwroot");
                        var wwwrootPath = Path.GetFullPath(projectRoot);
                        var pdfPath = Path.Combine(wwwrootPath, composition.Lien);

                        if (File.Exists(pdfPath))
                        {
                            try
                            {
                                _logger.LogInformation("Debut tentative Composition PDF");
                                using var reader = new PdfReader(pdfPath);
                                using var srcDoc = new PdfDocument(reader);

                                // Vérifiez si le PDF contient des pages avant de fusionner
                                if (srcDoc.GetNumberOfPages() > 0)
                                {
                                    var merger = new PdfMerger(document.GetPdfDocument());

                                    // Merge each page separately and add a page break after each page
                                    for (int i = 1; i <= srcDoc.GetNumberOfPages(); i++)
                                    {
                                        merger.Merge(srcDoc, i, i);
                                        AddFooter(document, modele);
                                        if (i < srcDoc.GetNumberOfPages()) // Avoid adding a page break after the last page of the document
                                        {
                                            document.Add(new AreaBreak(AreaBreakType.NEXT_PAGE));
                                        }
                                    }

                                    // Add a page break after the whole composition, if needed
                                    document.Add(new AreaBreak(AreaBreakType.NEXT_PAGE));
                                }
                                else
                                {
                                    _logger.LogError($"[ERROR] Le fichier PDF {pdfPath} est vide.");
                                }
                            }
                            catch (PdfException pdfEx)
                            {
                                _logger.LogError($"[ERROR] Erreur PDF lors de la fusion du fichier {composition.Lien}: {pdfEx.Message}");
                            }
                        }
                        else
                        {
                            _logger.LogError($"[ERROR] Fichier introuvable : {pdfPath}");
                        }
                    }
                }
                document.Add(new AreaBreak(AreaBreakType.NEXT_PAGE));

            }
            catch (Exception ex)
            {
                _logger.LogError($"[ERROR] Erreur lors de l'ajout des pages de composition : {ex.Message}");
                throw;
            }
        }
        public async Task AddPersonnelPage(Document document, Modele modele)
        {
            try
            {
                // Validation des entrées
                if (document == null)
                {
                    throw new ArgumentNullException(nameof(document));
                }
                if (modele?.Site == null)
                {
                    throw new ArgumentException("Modele or Site not found.");
                }

                // Ajout du titre
                var title = new Paragraph("INFORMATIONS")
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetFontSize(16)
                    .SetMarginTop(-25)
                    .SetMarginLeft(-5)
                    .SetFont(_boldFont);
                document.Add(title);
                var personnels = await _context.Personnels
                            .Include(p => p.PersonnelSites)
                            .AsNoTracking()
                            .Where(p => p.PersonnelSites.Any(ps => ps.SiteId == modele.Site.Id))
                            .ToListAsync();

                _logger.LogInformation($"Found {personnels.Count} personnel for site {modele.Site.Nom}");

                // Ajout des informations du personnel
                foreach (var personnel in personnels)
                {
                    if (string.IsNullOrEmpty(personnel.Prenom) || string.IsNullOrEmpty(personnel.Nom))
                    {
                        _logger.LogError("Invalid personnel data: Missing Prenom or Nom");
                        continue;
                    }

                    var paragraph = new Paragraph()
                        .Add(new Text($"{personnel.Prenom} {personnel.Nom}").SetFont(_boldFont))
                        .Add(new Text($" - {personnel.Description}\n"))
                        .Add(new Text($"\n{personnel.Telephone}"))
                        .Add(new Text($" - "))
                        .Add(new Text($"{personnel.Mail}\n").SetFontColor(ColorConstants.BLUE).SetUnderline())
                        .SetFontSize(10)
                        .SetMarginLeft(-23)
                        .SetMarginTop(30)
                        .SetMarginBottom(40);

                    document.Add(paragraph);
                }

                // Ajout des informations supplémentaires
                var link = new Text("https://formations.mayenne.cci.fr")
                    .SetUnderline()
                    .SetFontColor(ColorConstants.BLUE)
                    .SetFontSize(10);

                var additionalInfo = new Paragraph()
                    .Add(new Text("PLANNINGS, NOTES ET REFERENTIELS\n").SetFont(_boldFont).SetFontSize(12))
                    .Add(new Text("\nNet-Yparéo : ").SetFont(_boldFont))
                    .Add(link)
                    .Add(new Text("\n\nPortail web dédié au parcours de l'étudiant (accès planning, notes, référentiels, etc.)\n"))
                    .Add(new Text("\nIdentifiants : communiqués par mail"))
                    .SetFontSize(10)
                    .SetMarginLeft(-23)
                    .SetTextAlignment(TextAlignment.LEFT);

                document.Add(additionalInfo);

                // Ajout du pied de page et saut de page
                AddFooter(document, modele);
                document.Add(new AreaBreak(AreaBreakType.NEXT_PAGE));
            }
            catch (iText.Kernel.Exceptions.PdfException pdfEx)
            {
                _logger.LogError($"PDF Exception: {pdfEx.Message}");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError($"General Exception: {ex.Message}");
                _logger.LogError($"Stack Trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    _logger.LogError($"Inner Exception: {ex.InnerException.Message}");
                }
                throw;
            }
        }

        private async Task<List<(string nomMatiere, string formateur)>> LoadData(ulong groupeId)
        {
            var data = await _context.GroupeMatieres
                .Where(gm => gm.GroupeId == groupeId)
                .Join(_context.Matieres, gm => gm.MatiereId, m => m.Id, (gm, m) => new { m.Nom, gm.Matiere.FormateurId })
                .Join(_context.Formateurs, gm => gm.FormateurId, f => f.Id, (gm, f) => new { gm.Nom, Formateur = f.Prenom + " " + f.Nom })
                .Select(x => new ValueTuple<string, string>(x.Nom, x.Formateur))
                .ToListAsync();

            return data;
        }

        private async Task AddEquipePedagogiquePage(Document document, Modele modele)
        {
            try
            {
                var titleFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
                var blueColor = new DeviceRgb(60, 63, 235);

                var title = new Paragraph($"EQUIPE PEDAGOGIQUE - {modele.Groupe.Nom}")
                    .SetFont(titleFont)
                    .SetFontSize(15)
                    .SetMarginLeft(-20)
                    .SetMarginTop(-15)
                    .SetFontColor(blueColor);

                document.Add(title);
                document.Add(new Paragraph("\n"));
                var data = await LoadData(modele.GroupeId);

                var header = new List<string> { "Matière", "Formateur" };
                var columnWidths = new float[] { 1, 1 };

                AddColoredTable(document, header, data, columnWidths);

                AddFooter(document, modele);
                document.Add(new AreaBreak(AreaBreakType.NEXT_PAGE));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Erreur lors de l'ajout de la page d'équipe pédagogique : {ex.Message}");
                throw;
            }
        }

        private void AddCompteRenduPageAsync(Document document, Modele modele)
        {
            try
            {
                var userInfo = new Paragraph()
                    .SetFont(_defaultFont)
                    .SetFontSize(10)
                    .Add(new Text("Nom - Prénom : ").SetFont(_boldFont))
                    .SetMarginBottom(20)
                    .SetMarginTop(-20);
                document.Add(userInfo);

                var periode = new Paragraph()
                    .SetFont(_defaultFont)
                    .SetFontSize(10)
                    .Add(new Text("Période : ").SetFont(_boldFont))
                    .SetMarginBottom(35);
                document.Add(periode);

                var title = new Paragraph("Compte-Rendu d'Activités en Entreprise")
                    .SetFont(_boldFont)
                    .SetFontSize(10)
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetMarginBottom(40);
                document.Add(title);

                AddCompteRenduSection(document,
                    "Activités professionnelles confiées en entreprise",
                    "Missions, définitions et avancées des objectifs fixés, progrès en entreprise...,etc",
                    215);

                AddCompteRenduSection(document,
                    "Observations de l'apprenti",
                    " Principales découvertes, difficultés de compréhension, liens entre les connaissances et les activités en\r\n etreprise, etc",
                    150);

                AddCompteRenduSection(document,
                    "Observations du tuteur","",
                    100);

                AddCompteRenduSection(document,
                    "Observations du référent du groupe", "",
                    150);
                AddFooter(document, modele);

                document.Add(new AreaBreak(AreaBreakType.NEXT_PAGE));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Erreur lors de l'ajout de la page de compte-rendu : {ex.Message}");
                throw;
            }
        }

        private void AddCompteRenduSection(Document document, string title, string content, float height)
        {
            var cell = new Cell();

            var sectionTitle = new Paragraph()
                .SetFont(_boldFont)
                .SetFontSize(10)
                .SetMarginTop(-3)
                .Add(title);

            var sectionContent = new Paragraph()
                .SetFont(_defaultFont)
                .SetFontSize(10)
                .SetMarginTop(10)
                .Add(content);

            cell.Add(sectionTitle);
            cell.Add(sectionContent);

            var table = new Table(1)
                .SetWidth(UnitValue.CreatePercentValue(100));

            table.AddCell(cell);
            table.SetMinHeight(height);

            document.Add(table);
        }
        private void AddObservationsPage(Document document, Modele modele)
        {
            try
            {
                var apprentiTitle = new Paragraph("OBSERVATIONS DE L'APPRENTI")
                    .SetFont(_boldFont)
                    .SetFontSize(14)
                    .SetFontColor(new DeviceRgb(0, 88, 165))
                    .SetMarginBottom(170)
                    .SetMarginTop(-25);
                document.Add(apprentiTitle);

                var adminTitle = new Paragraph("OBSERVATIONS DU RESPONSABLE PEDAGOGIQUE")
                    .SetFont(_boldFont)
                    .SetFontSize(14)
                    .SetFontColor(new DeviceRgb(0, 88, 165));
                document.Add(adminTitle);

                AddFooter(document, modele);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Erreur lors de l'ajout des observations : {ex.Message}");
                throw;
            }
        }

        private void AddFooter(Document document, Modele modele)
        {
            var footer = new Paragraph("LIVRET D'APPRENTISSAGE / " + modele.Groupe.Nom)
                .SetFont(_italicFont)
                .SetFontColor(ColorConstants.BLACK)
                .SetFontSize(8)
                .SetTextAlignment(TextAlignment.LEFT)
                .SetFixedPosition(document.GetPdfDocument().GetNumberOfPages(), 30, 30, 500);
            document.Add(footer);
        }

        private void AddColoredTable(Document document, List<string> header, List<(string nomMatiere, string formateur)> data, float[] columnWidths)
        {
            var table = new Table(UnitValue.CreatePercentArray(columnWidths));
            table.SetWidth(UnitValue.CreatePercentValue(100));
            table.SetFontSize(10);

            foreach (var columnHeader in header)
            {
                var cell = new Cell()
                    .Add(new Paragraph(columnHeader)
                        .SetFont(_boldFont)
                        .SetFontColor(ColorConstants.WHITE)
                        .SetTextAlignment(TextAlignment.CENTER))
                    .SetBackgroundColor(ColorConstants.RED);

                table.AddCell(cell);
            }

            table.SetFont(_defaultFont);
            table.SetFontColor(ColorConstants.BLACK);

            bool fill = false;
            int rowIndex = 0;

            foreach (var row in data)
            {
                var matiereCell = new Cell()
                    .Add(new Paragraph(row.nomMatiere)
                        .SetTextAlignment(TextAlignment.CENTER))
                    .SetPadding(5)
                    .SetBorderBottom(Border.NO_BORDER)
                    .SetBorderTop(Border.NO_BORDER);

                var formateurCell = new Cell()
                    .Add(new Paragraph(row.formateur)
                        .SetTextAlignment(TextAlignment.CENTER))
                    .SetPadding(5)
                    .SetBorderBottom(Border.NO_BORDER)
                    .SetBorderTop(Border.NO_BORDER);

                if (fill)
                {
                    matiereCell.SetBackgroundColor(new DeviceRgb(224, 235, 255));
                    formateurCell.SetBackgroundColor(new DeviceRgb(224, 235, 255));
                }
                else
                {
                    matiereCell.SetBackgroundColor(ColorConstants.WHITE);
                    formateurCell.SetBackgroundColor(ColorConstants.WHITE);
                }

                if (rowIndex == data.Count - 1)
                {
                    matiereCell.SetBorderBottom(new SolidBorder(1));
                    formateurCell.SetBorderBottom(new SolidBorder(1));
                }

                table.AddCell(matiereCell);
                table.AddCell(formateurCell);

                fill = !fill;
                rowIndex++;
            }

            document.Add(table);
        }
    }
}
