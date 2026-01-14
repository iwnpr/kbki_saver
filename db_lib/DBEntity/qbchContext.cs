using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace db_lib.DBEntity;

public partial class qbchContext : DbContext
{
    public qbchContext(DbContextOptions<qbchContext> options)
        : base(options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        //optionsBuilder.UseNpgsql(_config.GetConnectionString("DataBase"));
    }

    public virtual DbSet<TdPermission> TdPermissions { get; set; }

    public virtual DbSet<TdUsersIndividual> TdUsersIndividuals { get; set; }

    public virtual DbSet<TdUsersLegal> TdUsersLegals { get; set; }

    public virtual DbSet<TeDlanswer> TeDlanswers { get; set; }

    public virtual DbSet<TeDlput> TeDlputs { get; set; }

    public virtual DbSet<TeDlputanswer> TeDlputanswers { get; set; }

    public virtual DbSet<TeDlrequest> TeDlrequests { get; set; }

    public virtual DbSet<TeQbchDlrequest> TeQbchDlrequests { get; set; }

    public virtual DbSet<TeSubject> TeSubjects { get; set; }

    public virtual DbSet<TeSubjectsDocument> TeSubjectsDocuments { get; set; }

    public virtual DbSet<TeSubjectsFullName> TeSubjectsFullNames { get; set; }

    public virtual DbSet<TrAbonent> TrAbonents { get; set; }

    public virtual DbSet<TrAbonentCertificate> TrAbonentCertificates { get; set; }

    public virtual DbSet<TrDlrequestType> TrDlrequestTypes { get; set; }

    public virtual DbSet<TrDocumentType> TrDocumentTypes { get; set; }

    public virtual DbSet<TrErrorCode> TrErrorCodes { get; set; }

    public virtual DbSet<TrQbchResponseType> TrQbchResponseTypes { get; set; }

    public virtual DbSet<TrService> TrServices { get; set; }

    public virtual DbSet<TrUserType> TrUserTypes { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TdPermission>(entity =>
        {
            entity.HasKey(e => e.KeyId).HasName("td_permission_pkey");

            entity.ToTable("td_permissions", "qbch");

            entity.Property(e => e.KeyId)
                .HasDefaultValueSql("nextval('qbch.td_permissions_key_id_seq1'::regclass)")
                .HasColumnName("key_id");
            entity.Property(e => e.AbonentsKeyId).HasColumnName("abonents_key_id");
            entity.Property(e => e.IsGranted)
                .HasDefaultValue(false)
                .HasColumnName("is_granted");
            entity.Property(e => e.ServicesKeyId).HasColumnName("services_key_id");
        });

        modelBuilder.Entity<TdUsersIndividual>(entity =>
        {
            entity.HasKey(e => e.KeyId).HasName("td_users_individual_pkey");

            entity.ToTable("td_users_individual", "qbch");

            entity.Property(e => e.KeyId)
                .HasDefaultValueSql("nextval('qbch.td_users_individual_key_id_seq2'::regclass)")
                .HasColumnName("key_id");
            entity.Property(e => e.BirthDate).HasColumnName("birth_date");
            entity.Property(e => e.BirthPlace).HasColumnName("birth_place");
            entity.Property(e => e.DocIssueDate).HasColumnName("doc_issue_date");
            entity.Property(e => e.DocIssuerCode).HasColumnName("doc_issuer_code");
            entity.Property(e => e.DocIssuerName).HasColumnName("doc_issuer_name");
            entity.Property(e => e.DocNumber).HasColumnName("doc_number");
            entity.Property(e => e.DocOtherName).HasColumnName("doc_other_name");
            entity.Property(e => e.DocSeria).HasColumnName("doc_seria");
            entity.Property(e => e.DocTypeKeyId).HasColumnName("doc_type_key_id");
            entity.Property(e => e.FirstName).HasColumnName("first_name");
            entity.Property(e => e.Inn).HasColumnName("inn");
            entity.Property(e => e.LastName).HasColumnName("last_name");
            entity.Property(e => e.MiddleName).HasColumnName("middle_name");
            entity.Property(e => e.Ogrn).HasColumnName("ogrn");
            entity.Property(e => e.Snils).HasColumnName("snils");
        });

        modelBuilder.Entity<TdUsersLegal>(entity =>
        {
            entity.HasKey(e => e.KeyId).HasName("td_users_legal_pkey");

            entity.ToTable("td_users_legal", "qbch");

            entity.Property(e => e.KeyId)
                .HasDefaultValueSql("nextval('qbch.td_users_legal_key_id_seq2'::regclass)")
                .HasColumnName("key_id");
            entity.Property(e => e.FullName).HasColumnName("full_name");
            entity.Property(e => e.Inn).HasColumnName("inn");
            entity.Property(e => e.IsForeign).HasColumnName("is_foreign");
            entity.Property(e => e.Ogrn).HasColumnName("ogrn");
            entity.Property(e => e.OtherName).HasColumnName("other_name");
            entity.Property(e => e.ShortName).HasColumnName("short_name");
        });

        modelBuilder.Entity<TeDlanswer>(entity =>
        {
            entity.HasKey(e => e.KeyId).HasName("te_dlanswers_pk");

            entity.ToTable("te_dlanswers", "qbch");

            entity.Property(e => e.KeyId)
                .HasDefaultValueSql("nextval('qbch.te_dlanswers_key_id_seq2'::regclass)")
                .HasColumnName("key_id");
            entity.Property(e => e.AbonentKeyId).HasColumnName("abonent_key_id");
            entity.Property(e => e.DlanswerId).HasColumnName("dlanswer_id");
            entity.Property(e => e.ErrorCodeKeyId).HasColumnName("error_code_key_id");
            entity.Property(e => e.ErrorMessage).HasColumnName("error_message");
            entity.Property(e => e.IpAddress).HasColumnName("ip_address");
            entity.Property(e => e.RequestCertificateThumbprint).HasColumnName("request_certificate_thumbprint");
            entity.Property(e => e.RequestDateTime)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("request_date_time");
            entity.Property(e => e.ResponseDateTime)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("response_date_time");
            entity.Property(e => e.ResponseSignedData).HasColumnName("response_signed_data");
            entity.Property(e => e.ResponseXml)
                .HasColumnType("xml")
                .HasColumnName("response_xml");
            entity.Property(e => e.TempGuid).HasColumnName("temp_guid");
            entity.Property(e => e.ValidationDateTime)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("validation_date_time");
        });

        modelBuilder.Entity<TeDlput>(entity =>
        {
            entity.HasKey(e => e.KeyId).HasName("te_dlrequests_pk_1");

            entity.ToTable("te_dlputs", "qbch");

            entity.Property(e => e.KeyId)
                .HasDefaultValueSql("nextval('qbch.te_dlputs_key_id_seq2'::regclass)")
                .HasColumnName("key_id");
            entity.Property(e => e.AbonentKeyId).HasColumnName("abonent_key_id");
            entity.Property(e => e.AddCommandsCount).HasColumnName("add_commands_count");
            entity.Property(e => e.DeleteCommandsCount).HasColumnName("delete_commands_count");
            entity.Property(e => e.DlputanswerId).HasColumnName("dlputanswer_id");
            entity.Property(e => e.ErrorCodeKeyId).HasColumnName("error_code_key_id");
            entity.Property(e => e.ErrorMessage).HasColumnName("error_message");
            entity.Property(e => e.IpAddress).HasColumnName("ip_address");
            entity.Property(e => e.RequestCertificateThumbprint).HasColumnName("request_certificate_thumbprint");
            entity.Property(e => e.RequestDateTime)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("request_date_time");
            entity.Property(e => e.RequestId).HasColumnName("request_id");
            entity.Property(e => e.RequestSignedData).HasColumnName("request_signed_data");
            entity.Property(e => e.RequestXml)
                .HasColumnType("xml")
                .HasColumnName("request_xml");
            entity.Property(e => e.ResponseDateTime)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("response_date_time");
            entity.Property(e => e.ResponseSignedData).HasColumnName("response_signed_data");
            entity.Property(e => e.ResponseXml)
                .HasColumnType("xml")
                .HasColumnName("response_xml");
            entity.Property(e => e.ValidationDateTime)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("validation_date_time");
        });

        modelBuilder.Entity<TeDlputanswer>(entity =>
        {
            entity.HasKey(e => e.KeyId).HasName("te_dlputanswers_pk");

            entity.ToTable("te_dlputanswers", "qbch");

            entity.Property(e => e.KeyId)
                .HasDefaultValueSql("nextval('qbch.te_dlputanswers_key_id_seq2'::regclass)")
                .HasColumnName("key_id");
            entity.Property(e => e.AbonentKeyId).HasColumnName("abonent_key_id");
            entity.Property(e => e.DlputanswerId).HasColumnName("dlputanswer_id");
            entity.Property(e => e.ErrorCodeKeyId).HasColumnName("error_code_key_id");
            entity.Property(e => e.ErrorMessage).HasColumnName("error_message");
            entity.Property(e => e.IpAddress).HasColumnName("ip_address");
            entity.Property(e => e.RequestCertificateThumbprint).HasColumnName("request_certificate_thumbprint");
            entity.Property(e => e.RequestDateTime)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("request_date_time");
            entity.Property(e => e.ResponseDateTime)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("response_date_time");
            entity.Property(e => e.ResponseSignedData).HasColumnName("response_signed_data");
            entity.Property(e => e.ResponseXml)
                .HasColumnType("xml")
                .HasColumnName("response_xml");
            entity.Property(e => e.TempGuid).HasColumnName("temp_guid");
            entity.Property(e => e.ValidationDateTime)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("validation_date_time");

        });

        modelBuilder.Entity<TeDlrequest>(entity =>
        {
            entity.HasKey(e => e.KeyId).HasName("te_dlrequests_pk");

            entity.ToTable("te_dlrequests", "qbch");

            entity.Property(e => e.KeyId)
                .HasDefaultValueSql("nextval('qbch.te_dlrequests_key_id_seq2'::regclass)")
                .HasColumnName("key_id");
            entity.Property(e => e.AbonentKeyId).HasColumnName("abonent_key_id");
            entity.Property(e => e.DlanswerId).HasColumnName("dlanswer_id");
            entity.Property(e => e.ErrorCodeKeyId).HasColumnName("error_code_key_id");
            entity.Property(e => e.ErrorMessage).HasColumnName("error_message");
            entity.Property(e => e.IpAddress).HasColumnName("ip_address");
            entity.Property(e => e.QbchTotalExecutionDateTime)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("qbch_total_execution_date_time");
            entity.Property(e => e.RequestCertificateThumbprint).HasColumnName("request_certificate_thumbprint");
            entity.Property(e => e.RequestDateTime)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("request_date_time");
            entity.Property(e => e.RequestId).HasColumnName("request_id");
            entity.Property(e => e.RequestSignedData).HasColumnName("request_signed_data");
            entity.Property(e => e.RequestXml)
                .HasColumnType("xml")
                .HasColumnName("request_xml");
            entity.Property(e => e.RequsetTypeKeyId).HasColumnName("requset_type_key_id");
            entity.Property(e => e.ResponseDateTime)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("response_date_time");
            entity.Property(e => e.ResponseSignedData).HasColumnName("response_signed_data");
            entity.Property(e => e.ResponseXml)
                .HasColumnType("xml")
                .HasColumnName("response_xml");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.UserTypeId).HasColumnName("user_type_id");
            entity.Property(e => e.ValidationDateTime)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("validation_date_time");

        });

        modelBuilder.Entity<TeQbchDlrequest>(entity =>
        {
            entity.HasKey(e => e.KeyId).HasName("te_qbch_dlrequests_pk");

            entity.ToTable("te_qbch_dlrequests", "qbch");

            entity.Property(e => e.KeyId)
                .HasDefaultValueSql("nextval('qbch.te_qbch_dlrequests_key_id_seq2'::regclass)")
                .HasColumnName("key_id");
            entity.Property(e => e.DlanswerResendCount).HasColumnName("dlanswer_resend_count");
            entity.Property(e => e.DlanswerStartDateTime)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("dlanswer_start_date_time");
            entity.Property(e => e.DlrequestMainKeyId).HasColumnName("dlrequest_main_key_id");
            entity.Property(e => e.DlrequestResendCount).HasColumnName("dlrequest_resend_count");
            entity.Property(e => e.DlrequestStartDateTime)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("dlrequest_start_date_time");
            entity.Property(e => e.ErrorCodeKeyId).HasColumnName("error_code_key_id");
            entity.Property(e => e.ErrorMessage).HasColumnName("error_message");
            entity.Property(e => e.QbchKeyId).HasColumnName("qbch_key_id");
            entity.Property(e => e.RequestSignedData).HasColumnName("request_signed_data");
            entity.Property(e => e.RequestXml)
                .HasColumnType("xml")
                .HasColumnName("request_xml");
            entity.Property(e => e.ResponseDateTime)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("response_date_time");
            entity.Property(e => e.ResponseId).HasColumnName("response_id");
            entity.Property(e => e.ResponseSignedData).HasColumnName("response_signed_data");
            entity.Property(e => e.ResponseType).HasColumnName("response_type");
            entity.Property(e => e.ResponseXml)
                .HasColumnType("xml")
                .HasColumnName("response_xml");
            entity.Property(e => e.TaskStartDateTime)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("task_start_date_time");
        });

        modelBuilder.Entity<TeSubject>(entity =>
        {
            entity.HasKey(e => e.KeyId).HasName("te_subjects_pkey");

            entity.ToTable("te_subjects", "qbch");

            entity.Property(e => e.KeyId)
                .HasDefaultValueSql("nextval('qbch.te_subjects_key_id_seq2'::regclass)")
                .HasColumnName("key_id");
            entity.Property(e => e.BirthDay).HasColumnName("birth_day");
            entity.Property(e => e.Inn).HasColumnName("inn");
            entity.Property(e => e.Psrn).HasColumnName("psrn");
            entity.Property(e => e.RequestKeyId).HasColumnName("request_key_id");
            entity.Property(e => e.Snils).HasColumnName("snils");
            
        });

        modelBuilder.Entity<TeSubjectsDocument>(entity =>
        {
            entity.HasKey(e => e.KeyId).HasName("te_subjects_documents_pkey");

            entity.ToTable("te_subjects_documents", "qbch");

            entity.Property(e => e.KeyId)
                .HasDefaultValueSql("nextval('qbch.te_subjects_documents_key_id_seq2'::regclass)")
                .HasColumnName("key_id");
            entity.Property(e => e.CountryCode).HasColumnName("country_code");
            entity.Property(e => e.DocDateIssue).HasColumnName("doc_date_issue");
            entity.Property(e => e.DocNumber).HasColumnName("doc_number");
            entity.Property(e => e.DocSeries).HasColumnName("doc_series");
            entity.Property(e => e.DocTypeKeyId).HasColumnName("doc_type_key_id");
            entity.Property(e => e.SubjectKeyId).HasColumnName("subject_key_id");

        });

        modelBuilder.Entity<TeSubjectsFullName>(entity =>
        {
            entity.HasKey(e => e.KeyId).HasName("te_subjects_full_name_pkey");

            entity.ToTable("te_subjects_full_name", "qbch");

            entity.Property(e => e.KeyId)
                .HasDefaultValueSql("nextval('qbch.te_subjects_full_name_key_id_seq2'::regclass)")
                .HasColumnName("key_id");
            entity.Property(e => e.FirstName).HasColumnName("first_name");
            entity.Property(e => e.LastName).HasColumnName("last_name");
            entity.Property(e => e.MiddleName).HasColumnName("middle_name");
            entity.Property(e => e.SubjectKeyId).HasColumnName("subject_key_id");

        });

        modelBuilder.Entity<TrAbonent>(entity =>
        {
            entity.HasKey(e => e.KeyId).HasName("tr_abonents_pkey");

            entity.ToTable("tr_abonents", "qbch");

            entity.Property(e => e.KeyId)
                .HasDefaultValueSql("nextval('qbch.tr_abonents_key_id_seq1'::regclass)")
                .HasColumnName("key_id");
            entity.Property(e => e.FullName).HasColumnName("full_name");
            entity.Property(e => e.Inn).HasColumnName("inn");
            entity.Property(e => e.Ogrn).HasColumnName("ogrn");
            entity.Property(e => e.ShortName).HasColumnName("short_name");
            entity.Property(e => e.UserTypeId).HasColumnName("user_type_id");

        });

        modelBuilder.Entity<TrAbonentCertificate>(entity =>
        {
            entity.HasKey(e => e.KeyId).HasName("tr_abonent_certificates_pkey");

            entity.ToTable("tr_abonent_certificates", "qbch");

            entity.Property(e => e.KeyId)
                .HasDefaultValueSql("nextval('qbch.tr_abonent_certificates_key_id_seq1'::regclass)")
                .HasColumnName("key_id");
            entity.Property(e => e.AbonentKeyId).HasColumnName("abonent_key_id");
            entity.Property(e => e.ExpirationDate)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("expiration_date");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(false)
                .HasColumnName("is_active");
            entity.Property(e => e.Thumbprint).HasColumnName("thumbprint");

        });

        modelBuilder.Entity<TrDlrequestType>(entity =>
        {
            entity.HasKey(e => e.KeyId).HasName("tr_dlrequest_types_pk");

            entity.ToTable("tr_dlrequest_types", "qbch");

            entity.Property(e => e.KeyId)
                .HasDefaultValueSql("nextval('qbch.tr_dlrequest_types_key_id_seq1'::regclass)")
                .HasColumnName("key_id");
            entity.Property(e => e.Description).HasColumnName("description");
        });

        modelBuilder.Entity<TrDocumentType>(entity =>
        {
            entity.HasKey(e => e.KeyId).HasName("tr_document_types_pk");

            entity.ToTable("tr_document_types", "qbch");

            entity.Property(e => e.KeyId).HasColumnName("key_id");
            entity.Property(e => e.DocDescription).HasColumnName("doc_description");
        });

        modelBuilder.Entity<TrErrorCode>(entity =>
        {
            entity.HasKey(e => e.KeyId).HasName("tr_error_codes_pkey");

            entity.ToTable("tr_error_codes", "qbch");

            entity.Property(e => e.KeyId)
                .HasDefaultValueSql("nextval('qbch.tr_error_codes_key_id_seq1'::regclass)")
                .HasColumnName("key_id");
            entity.Property(e => e.Comments).HasColumnName("comments");
            entity.Property(e => e.Description).HasColumnName("description");
        });

        modelBuilder.Entity<TrQbchResponseType>(entity =>
        {
            entity.HasKey(e => e.KeyId).HasName("tr_qbch_response_types_pk");

            entity.ToTable("tr_qbch_response_types", "qbch");

            entity.Property(e => e.KeyId).HasColumnName("key_id");
            entity.Property(e => e.ResponseData).HasColumnName("response_data");
            entity.Property(e => e.WhoseData).HasColumnName("whose_data");
        });

        modelBuilder.Entity<TrService>(entity =>
        {
            entity.HasKey(e => e.KeyId).HasName("tr_services_pkey");

            entity.ToTable("tr_services", "qbch");

            entity.Property(e => e.KeyId)
                .HasDefaultValueSql("nextval('qbch.tr_services_key_id_seq1'::regclass)")
                .HasColumnName("key_id");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.ServiceName).HasColumnName("service_name");
        });

        modelBuilder.Entity<TrUserType>(entity =>
        {
            entity.HasKey(e => e.KeyId).HasName("tr_user_types_pkey");

            entity.ToTable("tr_user_types", "qbch");

            entity.Property(e => e.KeyId)
                .HasDefaultValueSql("nextval('qbch.tr_user_types_key_id_seq1'::regclass)")
                .HasColumnName("key_id");
            entity.Property(e => e.Description).HasColumnName("description");
        });


        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
