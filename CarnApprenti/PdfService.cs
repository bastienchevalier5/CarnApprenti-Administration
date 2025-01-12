using Microsoft.EntityFrameworkCore;
using iText.Kernel.Font;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.IO.Font.Constants;
using iText.Kernel.Colors;
using iText.Kernel.Geom;
using Path = System.IO.Path;
using static CarnApprenti.LivretApprentissageContext;
using iText.Kernel.Utils;
using Cell = iText.Layout.Element.Cell;
using iText.Layout.Properties;
using TextAlignment = iText.Layout.Properties.TextAlignment;
using iText.Layout.Borders;
using Border = iText.Layout.Borders.Border;
using iText.Kernel.Exceptions;
using Microsoft.Extensions.Logging;


namespace CarnApprenti
{
    public class PdfService
    {
        private readonly LivretApprentissageContext _context;
        private readonly ILogger<DatabaseService> _logger;
        private PdfFont _defaultFont;
        private PdfFont _boldFont;
        private PdfFont _italicFont;

        public PdfService(LivretApprentissageContext context, ILogger<DatabaseService> logger)
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

        public async Task<byte[]> GeneratePdfAsync(ulong idModele, ulong idLivret)
        {
            try
            {
                var modele = await _context.Modeles.FindAsync(idModele);
                var groupe = await _context.Groupes.FindAsync(modele.GroupeId);

                using var memoryStream = new MemoryStream();
                var writerProperties = new WriterProperties().SetPdfVersion(PdfVersion.PDF_2_0);
                using var writer = new PdfWriter(memoryStream, writerProperties);
                using var pdf = new PdfDocument(writer);
                using var document = new Document(pdf, PageSize.A4);

                document.SetMargins(50, 50, 50, 50);

                await AddFirstPageAsync(document, modele.Id, idLivret);
                await AddCompositionPagesAsync(document, modele);
                await AddPersonnelPage(document, idLivret);
                await AddEquipePedagogiquePage(document, await GetGroupeAsync(modele.GroupeId), modele.GroupeId, idLivret);

                var comptesRendu = await GetComptesRenduAsync(idLivret);
                var livret = await _context.Livrets
                    .Include(l => l.User)
                    .FirstOrDefaultAsync(l => l.Id == idLivret)
                    ?? throw new InvalidOperationException($"Livret {idLivret} not found");

                if (comptesRendu?.Any() == true)
                {
                    foreach (var compte in comptesRendu)
                    {
                        await AddCompteRenduPageAsync(document, compte, livret.User);
                    }
                }

                AddObservationsPage(document, livret);

                // Add footer to the last page
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

        private async Task AddFirstPageAsync(Document document, ulong idModele, ulong idLivret)
        {
            try
            {
                
                    var modeles = await _context.Modeles
                        .Where(m => m.Id == idModele)
                        .FirstOrDefaultAsync();

                    var title = new Paragraph("Livret d'apprentissage")
                        .SetFont(_boldFont)
                        .SetFontColor(new DeviceRgb(60, 63, 235))
                        .SetFontSize(40)
                        .SetTextAlignment(TextAlignment.CENTER)
                        .SetMarginBottom(20)
                        .SetMarginTop(178);
                    document.Add(title);

                    var groupName = new Paragraph(await GetGroupeAsync(modeles.Groupe.Id))
                        .SetFont(_boldFont)
                        .SetFontColor(new DeviceRgb(60, 63, 235))
                        .SetFontSize(25)
                        .SetTextAlignment(TextAlignment.CENTER)
                        .SetMarginTop(0);
                    document.Add(groupName);

                var modele = await _context.Modeles
                    .Where(m => m.Id == idModele)
                    .FirstOrDefaultAsync();

                var livret = await _context.Livrets
                    .Where(l => l.Id == idLivret)
                    .Include(l => l.User)
                    .FirstOrDefaultAsync(l => l.ModeleId == idModele);

                var site = await _context.Sites
                    .Where(s => s.Id == modele.SiteId)
                    .FirstOrDefaultAsync();

                if (livret?.User != null)
                {
                    var nomParagraph = new Paragraph($"Nom : {livret.User.Nom}")
                        .SetFont(_boldFont)
                        .SetFontSize(14)
                        .SetTextAlignment(TextAlignment.LEFT)
                        .SetMarginTop(65)
                        .SetMarginLeft(-20);
                    document.Add(nomParagraph);

                    var prenomParagraph = new Paragraph($"Prénom : {livret.User.Prenom}")
                        .SetFont(_boldFont)
                        .SetFontSize(14)
                        .SetTextAlignment(TextAlignment.LEFT)
                        .SetMarginTop(5)
                        .SetMarginLeft(-20);
                    document.Add(prenomParagraph);

                    var siteName = await GetSiteAsync(site.Id);
                    var siteParagraph = new Paragraph($"Site : {siteName}")
                        .SetFont(_boldFont)
                        .SetFontSize(14)
                        .SetTextAlignment(TextAlignment.LEFT)
                        .SetMarginTop(5)
                        .SetMarginLeft(-20);
                    document.Add(siteParagraph);
                }

                AddFooter(document, modele);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Erreur lors de l'ajout de la première page : {ex.Message}");
                throw;
            }
        }    

        private async Task<string?> GetGroupeAsync(string nom)
        {
            throw new NotImplementedException();
        }

        private async Task<Modele> GetModeleAsync(ulong idModele)
        {
            try
            {
                return await _context.Modeles
                    .Where(m => m.Id == idModele)
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Erreur lors de la récupération du modèle : {ex.Message}");
                throw;
            }
        }

        private async Task<string> GetGroupeAsync(ulong groupId)
        {
            var group = await _context.Groupes.FindAsync(groupId);
            return group != null ? group.Nom : "Groupe Inconnu";
        }

        private async Task<string?> GetSiteAsync(ulong idSite)
        {
            try
            {
                var site = await _context.Sites.FindAsync(idSite);
                return site != null ? site.Nom : "Site Inconnu";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Erreur lors de la récupération du site : {ex.Message}");
                throw;
            }
        }

        private async Task<IEnumerable<CompteRendu>> GetComptesRenduAsync(ulong idLivret)
        {
            try
            {
                return await _context.CompteRendus
                    .Where(cr => cr.LivretId == idLivret)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Erreur lors de la récupération des comptes-rendus : {ex.Message}");
                throw;
            }
        }

        public void AddReports(Document document, List<CompteRendu> reports, User user)
        {
            foreach (var report in reports)
            {
                AddCompteRenduPageAsync(document, report, user).Wait();
            }
        }

        private async Task AddCompteRenduPageAsync(Document document, CompteRendu compteRendu, User user)
        {
            try
            {
                var userInfo = new Paragraph()
                    .SetFont(_defaultFont)
                    .SetFontSize(10)
                    .Add(new Text("Nom - Prénom : ").SetFont(_boldFont))
                    .Add($"{user.Nom} {user.Prenom}")
                    .SetMarginBottom(20)
                    .SetMarginTop(-20);
                document.Add(userInfo);

                var periode = new Paragraph()
                    .SetFont(_defaultFont)
                    .SetFontSize(10)
                    .Add(new Text("Période : ").SetFont(_boldFont))
                    .Add(compteRendu.Periode)
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
                    compteRendu.ActivitesPro,
                    215);

                AddCompteRenduSection(document,
                    "Observations de l'apprenti",
                    compteRendu.ObservationsApprenti,
                    150);

                AddCompteRenduSection(document,
                    "Observations du tuteur",
                    compteRendu.ObservationsTuteur,
                    100);

                AddCompteRenduSection(document,
                    "Observations du référent du groupe",
                    compteRendu.ObservationsReferent,
                    150);

                var livret = await _context.Livrets
                    .Where(l => l.User == user)
                    .FirstAsync();

                var modele = await _context.Modeles
                    .Where(m => m.Id == livret.Modele.Id)
                    .FirstAsync();
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

        public async Task AddObservationsPage(Document document, Livret livret)
        {
            try
            {
                // Récupération des observations
                var observations = _context.Livrets
                    .Where(l => l.Id == livret.Id)
                    .Select(l => new { l.ObservationApprentiGlobal, l.ObservationAdmin })
                    .FirstOrDefault();

                if (observations != null)
                {
                    // Titre des observations de l'apprenti
                    var apprentiTitle = new Paragraph("OBSERVATIONS DE L'APPRENTI")
                        .SetFont(_boldFont)
                        .SetFontSize(14)
                        .SetFontColor(new DeviceRgb(0, 88, 165))
                        .SetMarginBottom(5)
                        .SetMarginTop(-25); // Ajustez la marge si nécessaire
                    document.Add(apprentiTitle);

                    // Contenu des observations de l'apprenti
                    var apprentiContent = new Paragraph(observations.ObservationApprentiGlobal)
                        .SetFont(_defaultFont)
                        .SetFontSize(10)
                        .SetMarginBottom(165); // Ajustez la marge si nécessaire
                    document.Add(apprentiContent);

                    // Titre des observations du responsable pédagogique
                    var adminTitle = new Paragraph("OBSERVATIONS DU RESPONSABLE PEDAGOGIQUE")
                        .SetFont(_boldFont)
                        .SetFontSize(14)
                        .SetFontColor(new DeviceRgb(0, 88, 165))
                        .SetMarginBottom(5);
                    document.Add(adminTitle);

                    // Contenu des observations du responsable pédagogique
                    var adminContent = new Paragraph(observations.ObservationAdmin)
                        .SetFont(_defaultFont)
                        .SetFontSize(10)
                        .SetMarginBottom(165); // Ajustez la marge si nécessaire
                    document.Add(adminContent);
                }
                else
                {
                    var noObservations = new Paragraph("Aucune observation disponible.")
                        .SetFont(_defaultFont)
                        .SetFontSize(12);
                    document.Add(noObservations);
                }

                // Ajout du pied de page
                var modele = await _context.Modeles
                    .Where(m => m.Id == livret.Modele.Id)
                    .FirstAsync();
                AddFooter(document, modele);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Erreur lors de l'ajout des observations : {ex.Message}");
                throw;
            }
        }

        public void AddFooter(Document document, Modele modele)
        {
            string groupeNom = modele?.Groupe?.Nom ?? "Groupe Inconnu";
            var footer = new Paragraph("LIVRET D'APPRENTISSAGE / " + groupeNom)
                .SetFont(_italicFont)
                .SetFontColor(ColorConstants.BLACK)
                .SetFontSize(8)
                .SetTextAlignment(TextAlignment.LEFT)
                .SetFixedPosition(document.GetPdfDocument().GetNumberOfPages(), 30, 30, 500);
            document.Add(footer);
        }

        public async Task AddPersonnelPage(Document document, ulong idLivret)
        {
            try
            {
                var title = new Paragraph("INFORMATIONS")
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetFontSize(16)
                    .SetMarginTop(-25)
                    .SetMarginLeft(-5)
                    .SetFont(_boldFont);
                document.Add(title);

                var livret = await _context.Livrets
                    .Include(l => l.Modele)
                    .ThenInclude(m => m.Site)
                    .FirstOrDefaultAsync(l => l.Id == idLivret);

                if (livret == null)
                {
                    Console.WriteLine("Livret not found.");
                    return;
                }

                var modele = livret.Modele;
                if (modele == null)
                {
                    Console.WriteLine("Modele not found.");
                    return;
                }

                var site = modele.Site;
                if (site == null)
                {
                    Console.WriteLine("Site not found for the modele.");
                    return;
                }

                var personnels = await _context.Personnels
                    .Include(p => p.PersonnelSites)
                    .Where(p => p.PersonnelSites.Any(ps => ps.SiteId == site.Id))
                    .ToListAsync();

                Console.WriteLine($"Found {personnels.Count} personnel for site {site.Nom}");

                foreach (var personnel in personnels)
                {
                    if (string.IsNullOrEmpty(personnel.Prenom) || string.IsNullOrEmpty(personnel.Nom))
                    {
                        Console.WriteLine("Invalid personnel data: Missing Prenom or Nom");
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

                AddFooter(document, modele);

                document.Add(new AreaBreak(AreaBreakType.NEXT_PAGE));
            }
            catch (iText.Kernel.Exceptions.PdfException pdfEx)
            {
                Console.WriteLine($"PDF Exception: {pdfEx.Message}");
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"General Exception: {ex.Message}");
                throw;
            }
        }



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






        public async Task AddEquipePedagogiquePage(Document document, string groupeNom, ulong groupeId, ulong idLivret)
        {
            try
            {
                var titleFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
                var blueColor = new DeviceRgb(60, 63, 235);

                var title = new Paragraph($"EQUIPE PEDAGOGIQUE - {groupeNom}")
                    .SetFont(titleFont)
                    .SetFontSize(15)
                    .SetMarginLeft(-20)
                    .SetMarginTop(-15)
                    .SetFontColor(blueColor);

                document.Add(title);
                document.Add(new Paragraph("\n"));

                var data = await LoadData(groupeId);

                var header = new List<string> { "Matière", "Formateur" };
                var columnWidths = new float[] { 1, 1 };

                AddColoredTable(document, header, data, columnWidths);

                var livret = await _context.Livrets
                    .Where(l => l.Id == idLivret)
                    .FirstAsync();

                var modele = await _context.Modeles
                    .Where(m => m.Id == livret.Modele.Id)
                    .FirstAsync();
                AddFooter(document, modele);

                document.Add(new AreaBreak(AreaBreakType.NEXT_PAGE));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Erreur lors de l'ajout de la page d'équipe pédagogique : {ex.Message}");
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
