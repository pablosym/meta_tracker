using Microsoft.EntityFrameworkCore;
using Tracker.DTOs;

namespace Tracker.Models;

public partial class Tracker_DevelContext : DbContext
{
    public Tracker_DevelContext(){}

    public Tracker_DevelContext(DbContextOptions<Tracker_DevelContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Envio> Envios { get; set; } = null!;
    public virtual DbSet<Menue> Menues { get; set; } = null!;
    public virtual DbSet<MenuesRole> MenuesRoles { get; set; } = null!;

    public virtual DbSet<Parametrico> Parametricos { get; set; } = null!;
    public virtual DbSet<ParametricosHeader> ParametricosHeader { get; set; } = null!;
    public virtual DbSet<Usuario> Usuarios { get; set; } = null!;
    public virtual DbSet<UsuariosRole> UsuariosRoles { get; set; } = null!;
    public virtual DbSet<Chofer> Choferes { get; set; } = null!;

    public virtual DbSet<Vehiculo> Vehiculos { get; set; } = null!;

    public virtual DbSet<EnvioDTO> EnvioDTO { get; set; } = null!;

    public virtual DbSet<GuiaDTO> GuiaDTO { get; set; } = null!;

    public virtual DbSet<ArticuloDTO> ArticuloDTO { get; set; } = null!;

    public virtual DbSet<vwTransportista> vwTransportistas { get; set; } = null!;


    public virtual DbSet<EnvioGuia> EnviosGuias { get; set; } = null!;

    public virtual DbSet<EnvioAudit> EnviosAudit { get; set; } = null!;

    public virtual DbSet<Transportista> Transportistas { get; set; } = null!;


    public DbSet<vwTelefonoGuia> vwTelefonosGuias { get; set; }

    public DbSet<TelefonoGuiaLog> TelefonosGuiasLog { get; set; }

    public DbSet<TelefonoGuiaResultado> TelefonoGuiaResultado { get; set; }

    

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.UseCollation("Modern_Spanish_CI_AI");


        OnModelCreatingPartial(modelBuilder);


        modelBuilder.Entity<vwTransportista>(entity =>
        {
            entity.HasNoKey();

            entity.ToView("vwTransportistas");

            entity.Property(e => e.Codigo)
                .HasColumnType("numeric(5, 0)")
                .HasColumnName("CODIGO");

            entity.Property(e => e.Idprovlogi)
                .ValueGeneratedOnAdd()
                .HasColumnName("IDPROVLOGI");

            entity.Property(e => e.Nombre)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("NOMBRE");
        });

        modelBuilder.Entity<vwTelefonoGuia>()
              .ToView("vwTelefonosGuias")
              .HasNoKey();
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);

}
