using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Zmg.Infra.Migrations
{
    /// <inheritdoc />
    public partial class SpanishChecklistText : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "TemplateTaskTranslations",
                columns: new[] { "Locale", "TemplateTaskId", "Text" },
                values: new object[,]
                {
                    { "es", new Guid("11111111-1111-1111-1111-111111110100"), "Mezcla/master" },
                    { "es", new Guid("11111111-1111-1111-1111-111111110140"), "Configurar el smart link a todas las tiendas" },
                    { "es", new Guid("11111111-1111-1111-1111-111111110180"), "Meta ads: campaña inicial de lanzamiento" },
                    { "es", new Guid("11111111-1111-1111-1111-111111110201"), "Diseñar la portada para los DSPs" },
                    { "es", new Guid("11111111-1111-1111-1111-111111110241"), "Configurar la redirección del smart link desde zionmusicgroup.com/<song-name>" },
                    { "es", new Guid("11111111-1111-1111-1111-111111110281"), "Meta ads: campaña continua" },
                    { "es", new Guid("11111111-1111-1111-1111-111111110302"), "Distribuir a los DSPs" },
                    { "es", new Guid("11111111-1111-1111-1111-111111110342"), "Registrar la composición en BMI" },
                    { "es", new Guid("11111111-1111-1111-1111-111111110403"), "Hacer el video para YouTube, la miniatura y los demás recursos de YouTube" },
                    { "es", new Guid("11111111-1111-1111-1111-111111110443"), "Registrar la composición en MLC" },
                    { "es", new Guid("11111111-1111-1111-1111-111111110483"), "Anuncios de video en YouTube" },
                    { "es", new Guid("11111111-1111-1111-1111-111111110504"), "Pitch a Amazon" },
                    { "es", new Guid("11111111-1111-1111-1111-111111110544"), "Registrar en SoundExchange" },
                    { "es", new Guid("11111111-1111-1111-1111-111111110584"), "Anuncios en TikTok" },
                    { "es", new Guid("11111111-1111-1111-1111-111111110605"), "Pitch a Spotify" },
                    { "es", new Guid("11111111-1111-1111-1111-111111110645"), "Letra en Musixmatch: agregar/sincronizar" },
                    { "es", new Guid("11111111-1111-1111-1111-111111110685"), "Crear el video con letra para YouTube" },
                    { "es", new Guid("11111111-1111-1111-1111-111111110746"), "Revisar el lanzamiento en Deezer (artista equivocado)" },
                    { "es", new Guid("11111111-1111-1111-1111-111111110786"), "Preparar los multitracks: proyecto de Ableton, subida a Google Drive, nueva entrada en zionmusicgroup.com/recursos" },
                    { "es", new Guid("11111111-1111-1111-1111-111111110847"), "Revisar el lanzamiento en Amazon (artista equivocado)" },
                    { "es", new Guid("11111111-1111-1111-1111-111111110948"), "Revisar el lanzamiento en Apple (artista equivocado)" },
                    { "es", new Guid("11111111-1111-1111-1111-111111110c4b"), "Actualizar el banner de YouTube" },
                    { "es", new Guid("11111111-1111-1111-1111-111111110d4c"), "Actualizar el video destacado de YouTube" },
                    { "es", new Guid("11111111-1111-1111-1111-111111110e4d"), "Actualizar las tarjetas en los videos existentes" },
                    { "es", new Guid("11111111-1111-1111-1111-111111110f4e"), "Actualizar el comentario fijado en los videos existentes con el enlace al video nuevo" },
                    { "es", new Guid("11111111-1111-1111-1111-11111111104f"), "Actualizar el enlace de YouTube en las biografías de Instagram" },
                    { "es", new Guid("11111111-1111-1111-1111-111111111150"), "Actualizar la canción en las biografías de Instagram" },
                    { "es", new Guid("11111111-1111-1111-1111-111111111251"), "Enviar los master splits a los colaboradores" },
                    { "es", new Guid("22222222-2222-2222-2222-222222220100"), "Mezcla/master" },
                    { "es", new Guid("22222222-2222-2222-2222-222222220140"), "Configurar el smart link a todas las tiendas" },
                    { "es", new Guid("22222222-2222-2222-2222-222222220180"), "Meta ads: campaña inicial de lanzamiento" },
                    { "es", new Guid("22222222-2222-2222-2222-222222220201"), "Diseñar la portada para los DSPs" },
                    { "es", new Guid("22222222-2222-2222-2222-222222220241"), "Configurar la redirección del smart link desde zionmusicgroup.com/<song-name>" },
                    { "es", new Guid("22222222-2222-2222-2222-222222220281"), "Meta ads: campaña continua" },
                    { "es", new Guid("22222222-2222-2222-2222-222222220302"), "Distribuir a los DSPs" },
                    { "es", new Guid("22222222-2222-2222-2222-222222220342"), "Registrar la composición en BMI" },
                    { "es", new Guid("22222222-2222-2222-2222-222222220403"), "Hacer el video para YouTube, la miniatura y los demás recursos de YouTube" },
                    { "es", new Guid("22222222-2222-2222-2222-222222220443"), "Registrar la composición en MLC" },
                    { "es", new Guid("22222222-2222-2222-2222-222222220483"), "Anuncios de video en YouTube" },
                    { "es", new Guid("22222222-2222-2222-2222-222222220504"), "Pitch a Amazon" },
                    { "es", new Guid("22222222-2222-2222-2222-222222220544"), "Registrar en SoundExchange" },
                    { "es", new Guid("22222222-2222-2222-2222-222222220584"), "Anuncios en TikTok" },
                    { "es", new Guid("22222222-2222-2222-2222-222222220605"), "Pitch a Spotify" },
                    { "es", new Guid("22222222-2222-2222-2222-222222220645"), "Letra en Musixmatch: agregar/sincronizar" },
                    { "es", new Guid("22222222-2222-2222-2222-222222220685"), "Crear el video con letra para YouTube" },
                    { "es", new Guid("22222222-2222-2222-2222-222222220706"), "Cerrar el tracklist y el orden de las canciones (queda fijo al enviarlo a la distribuidora)" },
                    { "es", new Guid("22222222-2222-2222-2222-222222220746"), "Revisar el lanzamiento en Deezer (artista equivocado)" },
                    { "es", new Guid("22222222-2222-2222-2222-222222220786"), "Preparar los multitracks: proyecto de Ableton, subida a Google Drive, nueva entrada en zionmusicgroup.com/recursos" },
                    { "es", new Guid("22222222-2222-2222-2222-222222220807"), "Confirmar ISRC/UPC y los metadatos/créditos de cada canción" },
                    { "es", new Guid("22222222-2222-2222-2222-222222220847"), "Revisar el lanzamiento en Amazon (artista equivocado)" },
                    { "es", new Guid("22222222-2222-2222-2222-222222220887"), "Rotar los focus tracks cada pocas semanas con pitching de playlists por canción" },
                    { "es", new Guid("22222222-2222-2222-2222-222222220908"), "Elegir los focus tracks y planear 2-4 sencillos previos al álbum (waterfall: cada sencillo nuevo se reempaqueta con los anteriores y el álbum hereda sus streams)" },
                    { "es", new Guid("22222222-2222-2222-2222-222222220948"), "Revisar el lanzamiento en Apple (artista equivocado)" },
                    { "es", new Guid("22222222-2222-2222-2222-222222220988"), "Videos con letra para las canciones restantes" },
                    { "es", new Guid("22222222-2222-2222-2222-222222220a09"), "Campaña de pre-save del álbum" },
                    { "es", new Guid("22222222-2222-2222-2222-222222220b0a"), "Actualizar la biografía del artista / el comunicado de prensa / el EPK" },
                    { "es", new Guid("22222222-2222-2222-2222-222222220c0b"), "Producir contenido por lotes antes de la semana de lanzamiento (comentario canción por canción, videos con letra, versiones acústicas)" },
                    { "es", new Guid("22222222-2222-2222-2222-222222220c4b"), "Actualizar el banner de YouTube" },
                    { "es", new Guid("22222222-2222-2222-2222-222222220d0c"), "Medios físicos si aplica (los tiempos de producción de vinilo/CD son de meses)" },
                    { "es", new Guid("22222222-2222-2222-2222-222222220d4c"), "Actualizar el video destacado de YouTube" },
                    { "es", new Guid("22222222-2222-2222-2222-222222220e4d"), "Actualizar las tarjetas en los videos existentes" },
                    { "es", new Guid("22222222-2222-2222-2222-222222220f4e"), "Actualizar el comentario fijado en los videos existentes con el enlace al video nuevo" },
                    { "es", new Guid("22222222-2222-2222-2222-22222222104f"), "Actualizar el enlace de YouTube en las biografías de Instagram" },
                    { "es", new Guid("22222222-2222-2222-2222-222222221150"), "Actualizar la canción en las biografías de Instagram" },
                    { "es", new Guid("22222222-2222-2222-2222-222222221251"), "Enviar los master splits a los colaboradores" },
                    { "es", new Guid("22222222-2222-2222-2222-222222221352"), "Los registros (BMI, MLC, Musixmatch, splits) se repiten por cada canción" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "TemplateTaskTranslations",
                keyColumns: new[] { "Locale", "TemplateTaskId" },
                keyValues: new object[] { "es", new Guid("11111111-1111-1111-1111-111111110100") });

            migrationBuilder.DeleteData(
                table: "TemplateTaskTranslations",
                keyColumns: new[] { "Locale", "TemplateTaskId" },
                keyValues: new object[] { "es", new Guid("11111111-1111-1111-1111-111111110140") });

            migrationBuilder.DeleteData(
                table: "TemplateTaskTranslations",
                keyColumns: new[] { "Locale", "TemplateTaskId" },
                keyValues: new object[] { "es", new Guid("11111111-1111-1111-1111-111111110180") });

            migrationBuilder.DeleteData(
                table: "TemplateTaskTranslations",
                keyColumns: new[] { "Locale", "TemplateTaskId" },
                keyValues: new object[] { "es", new Guid("11111111-1111-1111-1111-111111110201") });

            migrationBuilder.DeleteData(
                table: "TemplateTaskTranslations",
                keyColumns: new[] { "Locale", "TemplateTaskId" },
                keyValues: new object[] { "es", new Guid("11111111-1111-1111-1111-111111110241") });

            migrationBuilder.DeleteData(
                table: "TemplateTaskTranslations",
                keyColumns: new[] { "Locale", "TemplateTaskId" },
                keyValues: new object[] { "es", new Guid("11111111-1111-1111-1111-111111110281") });

            migrationBuilder.DeleteData(
                table: "TemplateTaskTranslations",
                keyColumns: new[] { "Locale", "TemplateTaskId" },
                keyValues: new object[] { "es", new Guid("11111111-1111-1111-1111-111111110302") });

            migrationBuilder.DeleteData(
                table: "TemplateTaskTranslations",
                keyColumns: new[] { "Locale", "TemplateTaskId" },
                keyValues: new object[] { "es", new Guid("11111111-1111-1111-1111-111111110342") });

            migrationBuilder.DeleteData(
                table: "TemplateTaskTranslations",
                keyColumns: new[] { "Locale", "TemplateTaskId" },
                keyValues: new object[] { "es", new Guid("11111111-1111-1111-1111-111111110403") });

            migrationBuilder.DeleteData(
                table: "TemplateTaskTranslations",
                keyColumns: new[] { "Locale", "TemplateTaskId" },
                keyValues: new object[] { "es", new Guid("11111111-1111-1111-1111-111111110443") });

            migrationBuilder.DeleteData(
                table: "TemplateTaskTranslations",
                keyColumns: new[] { "Locale", "TemplateTaskId" },
                keyValues: new object[] { "es", new Guid("11111111-1111-1111-1111-111111110483") });

            migrationBuilder.DeleteData(
                table: "TemplateTaskTranslations",
                keyColumns: new[] { "Locale", "TemplateTaskId" },
                keyValues: new object[] { "es", new Guid("11111111-1111-1111-1111-111111110504") });

            migrationBuilder.DeleteData(
                table: "TemplateTaskTranslations",
                keyColumns: new[] { "Locale", "TemplateTaskId" },
                keyValues: new object[] { "es", new Guid("11111111-1111-1111-1111-111111110544") });

            migrationBuilder.DeleteData(
                table: "TemplateTaskTranslations",
                keyColumns: new[] { "Locale", "TemplateTaskId" },
                keyValues: new object[] { "es", new Guid("11111111-1111-1111-1111-111111110584") });

            migrationBuilder.DeleteData(
                table: "TemplateTaskTranslations",
                keyColumns: new[] { "Locale", "TemplateTaskId" },
                keyValues: new object[] { "es", new Guid("11111111-1111-1111-1111-111111110605") });

            migrationBuilder.DeleteData(
                table: "TemplateTaskTranslations",
                keyColumns: new[] { "Locale", "TemplateTaskId" },
                keyValues: new object[] { "es", new Guid("11111111-1111-1111-1111-111111110645") });

            migrationBuilder.DeleteData(
                table: "TemplateTaskTranslations",
                keyColumns: new[] { "Locale", "TemplateTaskId" },
                keyValues: new object[] { "es", new Guid("11111111-1111-1111-1111-111111110685") });

            migrationBuilder.DeleteData(
                table: "TemplateTaskTranslations",
                keyColumns: new[] { "Locale", "TemplateTaskId" },
                keyValues: new object[] { "es", new Guid("11111111-1111-1111-1111-111111110746") });

            migrationBuilder.DeleteData(
                table: "TemplateTaskTranslations",
                keyColumns: new[] { "Locale", "TemplateTaskId" },
                keyValues: new object[] { "es", new Guid("11111111-1111-1111-1111-111111110786") });

            migrationBuilder.DeleteData(
                table: "TemplateTaskTranslations",
                keyColumns: new[] { "Locale", "TemplateTaskId" },
                keyValues: new object[] { "es", new Guid("11111111-1111-1111-1111-111111110847") });

            migrationBuilder.DeleteData(
                table: "TemplateTaskTranslations",
                keyColumns: new[] { "Locale", "TemplateTaskId" },
                keyValues: new object[] { "es", new Guid("11111111-1111-1111-1111-111111110948") });

            migrationBuilder.DeleteData(
                table: "TemplateTaskTranslations",
                keyColumns: new[] { "Locale", "TemplateTaskId" },
                keyValues: new object[] { "es", new Guid("11111111-1111-1111-1111-111111110c4b") });

            migrationBuilder.DeleteData(
                table: "TemplateTaskTranslations",
                keyColumns: new[] { "Locale", "TemplateTaskId" },
                keyValues: new object[] { "es", new Guid("11111111-1111-1111-1111-111111110d4c") });

            migrationBuilder.DeleteData(
                table: "TemplateTaskTranslations",
                keyColumns: new[] { "Locale", "TemplateTaskId" },
                keyValues: new object[] { "es", new Guid("11111111-1111-1111-1111-111111110e4d") });

            migrationBuilder.DeleteData(
                table: "TemplateTaskTranslations",
                keyColumns: new[] { "Locale", "TemplateTaskId" },
                keyValues: new object[] { "es", new Guid("11111111-1111-1111-1111-111111110f4e") });

            migrationBuilder.DeleteData(
                table: "TemplateTaskTranslations",
                keyColumns: new[] { "Locale", "TemplateTaskId" },
                keyValues: new object[] { "es", new Guid("11111111-1111-1111-1111-11111111104f") });

            migrationBuilder.DeleteData(
                table: "TemplateTaskTranslations",
                keyColumns: new[] { "Locale", "TemplateTaskId" },
                keyValues: new object[] { "es", new Guid("11111111-1111-1111-1111-111111111150") });

            migrationBuilder.DeleteData(
                table: "TemplateTaskTranslations",
                keyColumns: new[] { "Locale", "TemplateTaskId" },
                keyValues: new object[] { "es", new Guid("11111111-1111-1111-1111-111111111251") });

            migrationBuilder.DeleteData(
                table: "TemplateTaskTranslations",
                keyColumns: new[] { "Locale", "TemplateTaskId" },
                keyValues: new object[] { "es", new Guid("22222222-2222-2222-2222-222222220100") });

            migrationBuilder.DeleteData(
                table: "TemplateTaskTranslations",
                keyColumns: new[] { "Locale", "TemplateTaskId" },
                keyValues: new object[] { "es", new Guid("22222222-2222-2222-2222-222222220140") });

            migrationBuilder.DeleteData(
                table: "TemplateTaskTranslations",
                keyColumns: new[] { "Locale", "TemplateTaskId" },
                keyValues: new object[] { "es", new Guid("22222222-2222-2222-2222-222222220180") });

            migrationBuilder.DeleteData(
                table: "TemplateTaskTranslations",
                keyColumns: new[] { "Locale", "TemplateTaskId" },
                keyValues: new object[] { "es", new Guid("22222222-2222-2222-2222-222222220201") });

            migrationBuilder.DeleteData(
                table: "TemplateTaskTranslations",
                keyColumns: new[] { "Locale", "TemplateTaskId" },
                keyValues: new object[] { "es", new Guid("22222222-2222-2222-2222-222222220241") });

            migrationBuilder.DeleteData(
                table: "TemplateTaskTranslations",
                keyColumns: new[] { "Locale", "TemplateTaskId" },
                keyValues: new object[] { "es", new Guid("22222222-2222-2222-2222-222222220281") });

            migrationBuilder.DeleteData(
                table: "TemplateTaskTranslations",
                keyColumns: new[] { "Locale", "TemplateTaskId" },
                keyValues: new object[] { "es", new Guid("22222222-2222-2222-2222-222222220302") });

            migrationBuilder.DeleteData(
                table: "TemplateTaskTranslations",
                keyColumns: new[] { "Locale", "TemplateTaskId" },
                keyValues: new object[] { "es", new Guid("22222222-2222-2222-2222-222222220342") });

            migrationBuilder.DeleteData(
                table: "TemplateTaskTranslations",
                keyColumns: new[] { "Locale", "TemplateTaskId" },
                keyValues: new object[] { "es", new Guid("22222222-2222-2222-2222-222222220403") });

            migrationBuilder.DeleteData(
                table: "TemplateTaskTranslations",
                keyColumns: new[] { "Locale", "TemplateTaskId" },
                keyValues: new object[] { "es", new Guid("22222222-2222-2222-2222-222222220443") });

            migrationBuilder.DeleteData(
                table: "TemplateTaskTranslations",
                keyColumns: new[] { "Locale", "TemplateTaskId" },
                keyValues: new object[] { "es", new Guid("22222222-2222-2222-2222-222222220483") });

            migrationBuilder.DeleteData(
                table: "TemplateTaskTranslations",
                keyColumns: new[] { "Locale", "TemplateTaskId" },
                keyValues: new object[] { "es", new Guid("22222222-2222-2222-2222-222222220504") });

            migrationBuilder.DeleteData(
                table: "TemplateTaskTranslations",
                keyColumns: new[] { "Locale", "TemplateTaskId" },
                keyValues: new object[] { "es", new Guid("22222222-2222-2222-2222-222222220544") });

            migrationBuilder.DeleteData(
                table: "TemplateTaskTranslations",
                keyColumns: new[] { "Locale", "TemplateTaskId" },
                keyValues: new object[] { "es", new Guid("22222222-2222-2222-2222-222222220584") });

            migrationBuilder.DeleteData(
                table: "TemplateTaskTranslations",
                keyColumns: new[] { "Locale", "TemplateTaskId" },
                keyValues: new object[] { "es", new Guid("22222222-2222-2222-2222-222222220605") });

            migrationBuilder.DeleteData(
                table: "TemplateTaskTranslations",
                keyColumns: new[] { "Locale", "TemplateTaskId" },
                keyValues: new object[] { "es", new Guid("22222222-2222-2222-2222-222222220645") });

            migrationBuilder.DeleteData(
                table: "TemplateTaskTranslations",
                keyColumns: new[] { "Locale", "TemplateTaskId" },
                keyValues: new object[] { "es", new Guid("22222222-2222-2222-2222-222222220685") });

            migrationBuilder.DeleteData(
                table: "TemplateTaskTranslations",
                keyColumns: new[] { "Locale", "TemplateTaskId" },
                keyValues: new object[] { "es", new Guid("22222222-2222-2222-2222-222222220706") });

            migrationBuilder.DeleteData(
                table: "TemplateTaskTranslations",
                keyColumns: new[] { "Locale", "TemplateTaskId" },
                keyValues: new object[] { "es", new Guid("22222222-2222-2222-2222-222222220746") });

            migrationBuilder.DeleteData(
                table: "TemplateTaskTranslations",
                keyColumns: new[] { "Locale", "TemplateTaskId" },
                keyValues: new object[] { "es", new Guid("22222222-2222-2222-2222-222222220786") });

            migrationBuilder.DeleteData(
                table: "TemplateTaskTranslations",
                keyColumns: new[] { "Locale", "TemplateTaskId" },
                keyValues: new object[] { "es", new Guid("22222222-2222-2222-2222-222222220807") });

            migrationBuilder.DeleteData(
                table: "TemplateTaskTranslations",
                keyColumns: new[] { "Locale", "TemplateTaskId" },
                keyValues: new object[] { "es", new Guid("22222222-2222-2222-2222-222222220847") });

            migrationBuilder.DeleteData(
                table: "TemplateTaskTranslations",
                keyColumns: new[] { "Locale", "TemplateTaskId" },
                keyValues: new object[] { "es", new Guid("22222222-2222-2222-2222-222222220887") });

            migrationBuilder.DeleteData(
                table: "TemplateTaskTranslations",
                keyColumns: new[] { "Locale", "TemplateTaskId" },
                keyValues: new object[] { "es", new Guid("22222222-2222-2222-2222-222222220908") });

            migrationBuilder.DeleteData(
                table: "TemplateTaskTranslations",
                keyColumns: new[] { "Locale", "TemplateTaskId" },
                keyValues: new object[] { "es", new Guid("22222222-2222-2222-2222-222222220948") });

            migrationBuilder.DeleteData(
                table: "TemplateTaskTranslations",
                keyColumns: new[] { "Locale", "TemplateTaskId" },
                keyValues: new object[] { "es", new Guid("22222222-2222-2222-2222-222222220988") });

            migrationBuilder.DeleteData(
                table: "TemplateTaskTranslations",
                keyColumns: new[] { "Locale", "TemplateTaskId" },
                keyValues: new object[] { "es", new Guid("22222222-2222-2222-2222-222222220a09") });

            migrationBuilder.DeleteData(
                table: "TemplateTaskTranslations",
                keyColumns: new[] { "Locale", "TemplateTaskId" },
                keyValues: new object[] { "es", new Guid("22222222-2222-2222-2222-222222220b0a") });

            migrationBuilder.DeleteData(
                table: "TemplateTaskTranslations",
                keyColumns: new[] { "Locale", "TemplateTaskId" },
                keyValues: new object[] { "es", new Guid("22222222-2222-2222-2222-222222220c0b") });

            migrationBuilder.DeleteData(
                table: "TemplateTaskTranslations",
                keyColumns: new[] { "Locale", "TemplateTaskId" },
                keyValues: new object[] { "es", new Guid("22222222-2222-2222-2222-222222220c4b") });

            migrationBuilder.DeleteData(
                table: "TemplateTaskTranslations",
                keyColumns: new[] { "Locale", "TemplateTaskId" },
                keyValues: new object[] { "es", new Guid("22222222-2222-2222-2222-222222220d0c") });

            migrationBuilder.DeleteData(
                table: "TemplateTaskTranslations",
                keyColumns: new[] { "Locale", "TemplateTaskId" },
                keyValues: new object[] { "es", new Guid("22222222-2222-2222-2222-222222220d4c") });

            migrationBuilder.DeleteData(
                table: "TemplateTaskTranslations",
                keyColumns: new[] { "Locale", "TemplateTaskId" },
                keyValues: new object[] { "es", new Guid("22222222-2222-2222-2222-222222220e4d") });

            migrationBuilder.DeleteData(
                table: "TemplateTaskTranslations",
                keyColumns: new[] { "Locale", "TemplateTaskId" },
                keyValues: new object[] { "es", new Guid("22222222-2222-2222-2222-222222220f4e") });

            migrationBuilder.DeleteData(
                table: "TemplateTaskTranslations",
                keyColumns: new[] { "Locale", "TemplateTaskId" },
                keyValues: new object[] { "es", new Guid("22222222-2222-2222-2222-22222222104f") });

            migrationBuilder.DeleteData(
                table: "TemplateTaskTranslations",
                keyColumns: new[] { "Locale", "TemplateTaskId" },
                keyValues: new object[] { "es", new Guid("22222222-2222-2222-2222-222222221150") });

            migrationBuilder.DeleteData(
                table: "TemplateTaskTranslations",
                keyColumns: new[] { "Locale", "TemplateTaskId" },
                keyValues: new object[] { "es", new Guid("22222222-2222-2222-2222-222222221251") });

            migrationBuilder.DeleteData(
                table: "TemplateTaskTranslations",
                keyColumns: new[] { "Locale", "TemplateTaskId" },
                keyValues: new object[] { "es", new Guid("22222222-2222-2222-2222-222222221352") });
        }
    }
}
