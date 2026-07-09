using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace db_lib.Entities;

public partial class QbchContext : DbContext
{
    public QbchContext()
    {
    }

    public QbchContext(DbContextOptions<QbchContext> options)
        : base(options)
    {
    }

    public virtual DbSet<TdPermission> TdPermissions { get; set; }

    public virtual DbSet<TdUser> TdUsers { get; set; }

    public virtual DbSet<TeCertManage> TeCertManages { get; set; }

    public virtual DbSet<TeDlanswer> TeDlanswers { get; set; }

    public virtual DbSet<TeDlput> TeDlputs { get; set; }

    public virtual DbSet<TeDlputanswer> TeDlputanswers { get; set; }

    public virtual DbSet<TeDlrequest> TeDlrequests { get; set; }

    public virtual DbSet<TeQbchDlanswer> TeQbchDlanswers { get; set; }

    public virtual DbSet<TeQbchDlrequest> TeQbchDlrequests { get; set; }

    public virtual DbSet<TeQbchTask> TeQbchTasks { get; set; }

    public virtual DbSet<TeConsentPurpose> TeConsentPurposes { get; set; }

    public virtual DbSet<TeRequest> TeRequests { get; set; }

    public virtual DbSet<TeRequestPurpose> TeRequestPurposes { get; set; }

    public virtual DbSet<TeResponse> TeResponses { get; set; }

    public virtual DbSet<TeSubject> TeSubjects { get; set; }

    public virtual DbSet<TeSubjectsDocument> TeSubjectsDocuments { get; set; }

    public virtual DbSet<TeSubjectsFullName> TeSubjectsFullNames { get; set; }

    public virtual DbSet<TrAbonent> TrAbonents { get; set; }

    public virtual DbSet<TrAbonentCertificate> TrAbonentCertificates { get; set; }

    public virtual DbSet<TrAmpResponseType> TrAmpResponseTypes { get; set; }

    public virtual DbSet<TrDlrequestType> TrDlrequestTypes { get; set; }

    public virtual DbSet<TrDocumentType> TrDocumentTypes { get; set; }

    public virtual DbSet<TrErrorCode> TrErrorCodes { get; set; }

    public virtual DbSet<TrInformationCode> TrInformationCodes { get; set; }

    public virtual DbSet<TrRequestMode> TrRequestModes { get; set; }

    public virtual DbSet<TrService> TrServices { get; set; }

    public virtual DbSet<TrSpResponseType> TrSpResponseTypes { get; set; }

    public virtual DbSet<TrUserType> TrUserTypes { get; set; }

    public virtual DbSet<TrUserTypeCode> TrUserTypeCodes { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("qbchv3");

        modelBuilder.Entity<TdPermission>(entity =>
        {
            entity.HasKey(e => e.KeyId).HasName("td_permission_pkey");

            entity.ToTable("td_permissions", "qbchv3", tb => tb.HasComment("Права доступа к запросам КБКИ"));

            entity.Property(e => e.KeyId)
                .HasDefaultValueSql("nextval('qbchv3.td_permissions_key_id_seq1'::regclass)")
                .HasColumnName("key_id");
            entity.Property(e => e.AbonentId).HasColumnName("abonent_id");
            entity.Property(e => e.Inserted)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("inserted");
            entity.Property(e => e.IsGranted)
                .HasDefaultValue(false)
                .HasColumnName("is_granted");
            entity.Property(e => e.ServiceId).HasColumnName("service_id");

            entity.HasOne(d => d.Abonent).WithMany(p => p.TdPermissions)
                .HasForeignKey(d => d.AbonentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("td_permissions_abonents_fk");

            entity.HasOne(d => d.Service).WithMany(p => p.TdPermissions)
                .HasForeignKey(d => d.ServiceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("td_permissions_service_fk");
        });

        modelBuilder.Entity<TdUser>(entity =>
        {
            entity.HasKey(e => e.KeyId).HasName("td_users_pk");

            entity.ToTable("td_users", "qbchv3", tb => tb.HasComment("Список пользователей - контрагентов. Здесь присутствуют как контрагенты КБКИ так и внешние, которые пришли к нам с запросом."));

            entity.Property(e => e.KeyId)
                .HasDefaultValueSql("nextval('qbchv3.td_users_key_id_seq1'::regclass)")
                .HasColumnName("key_id");
            entity.Property(e => e.BirthDate).HasColumnName("birth_date");
            entity.Property(e => e.BirthPlace).HasColumnName("birth_place");
            entity.Property(e => e.DocIssueDate).HasColumnName("doc_issue_date");
            entity.Property(e => e.DocIssuerCode).HasColumnName("doc_issuer_code");
            entity.Property(e => e.DocIssuerName).HasColumnName("doc_issuer_name");
            entity.Property(e => e.DocNumber).HasColumnName("doc_number");
            entity.Property(e => e.DocOtherName).HasColumnName("doc_other_name");
            entity.Property(e => e.DocSeria).HasColumnName("doc_seria");
            entity.Property(e => e.DocType).HasColumnName("doc_type");
            entity.Property(e => e.FirstName).HasColumnName("first_name");
            entity.Property(e => e.FullName).HasColumnName("full_name");
            entity.Property(e => e.Inn).HasColumnName("inn");
            entity.Property(e => e.Inserted)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("inserted");
            entity.Property(e => e.IsForeign).HasColumnName("is_foreign");
            entity.Property(e => e.LastName).HasColumnName("last_name");
            entity.Property(e => e.MiddleName).HasColumnName("middle_name");
            entity.Property(e => e.Ogrn).HasColumnName("ogrn");
            entity.Property(e => e.OtherName).HasColumnName("other_name");
            entity.Property(e => e.ShortName).HasColumnName("short_name");
            entity.Property(e => e.Snils).HasColumnName("snils");
            entity.Property(e => e.UserType).HasColumnName("user_type");
            //entity.Property(e => e.UserTypeCodeId).HasColumnName("user_type_code_id");

            entity.HasOne(d => d.DocTypeNavigation).WithMany(p => p.TdUsers)
                .HasForeignKey(d => d.DocType)
                .HasConstraintName("td_users_doc_types_fk");

            entity.HasOne(d => d.UserTypeNavigation).WithMany(p => p.TdUsers)
                .HasForeignKey(d => d.UserType)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("td_users_user_types_fk");
        });

        modelBuilder.Entity<TeCertManage>(entity =>
        {
            entity.HasKey(e => e.KeyId).HasName("te_cert_manage_pk");

            entity.ToTable("te_cert_manage", "qbchv3", tb => tb.HasComment("Таблица используется для запросов cert_add и cert_revoke"));

            entity.Property(e => e.KeyId)
                .HasDefaultValueSql("nextval('qbchv3.te_cert_manage_key_id_seq1'::regclass)")
                .HasColumnName("key_id");
            entity.Property(e => e.AbonentId).HasColumnName("abonent_id");
            entity.Property(e => e.CertData).HasColumnName("cert_data");
            entity.Property(e => e.CertificateId).HasColumnName("certificate_id");
            entity.Property(e => e.ErrorCode)
                .HasDefaultValue(0)
                .HasColumnName("error_code");
            entity.Property(e => e.ErrorMessage).HasColumnName("error_message");
            entity.Property(e => e.Inserted)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("inserted");
            entity.Property(e => e.IpAddress).HasColumnName("ip_address");
            entity.Property(e => e.RequestCertificateThumbprint).HasColumnName("request_certificate_thumbprint");
            entity.Property(e => e.RequestDateTime)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("request_date_time");
            entity.Property(e => e.RequestId).HasColumnName("request_id");
            entity.Property(e => e.ResponseDateTime)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("response_date_time");
            entity.Property(e => e.ResponseSignedData).HasColumnName("response_signed_data");
            entity.Property(e => e.ResponseXml)
                .HasColumnType("xml")
                .HasColumnName("response_xml");
            entity.Property(e => e.ServiceType).HasColumnName("service_type");
            entity.Property(e => e.SignData).HasColumnName("sign_data");
            entity.Property(e => e.TempGuid).HasColumnName("temp_guid");
            entity.Property(e => e.ValidationDateTime)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("validation_date_time");

            entity.HasOne(d => d.Abonent).WithMany(p => p.TeCertManages)
                .HasForeignKey(d => d.AbonentId)
                .HasConstraintName("te_cert_manage_abonents_fk");

            entity.HasOne(d => d.Certificate).WithMany(p => p.TeCertManages)
                .HasForeignKey(d => d.CertificateId)
                .HasConstraintName("te_cert_manage_abonents_certificates_fk");
        });

        modelBuilder.Entity<TeDlanswer>(entity =>
        {
            entity.HasKey(e => e.KeyId).HasName("te_dlanswers_pk");

            entity.ToTable("te_dlanswers", "qbchv3", tb => tb.HasComment("Таблица со всеми запросами dlanswer, которые пришли к нам."));

            entity.Property(e => e.KeyId)
                .HasDefaultValueSql("nextval('qbchv3.te_dlanswers_key_id_seq1'::regclass)")
                .HasColumnName("key_id");
            entity.Property(e => e.AbonentId).HasColumnName("abonent_id");
            entity.Property(e => e.ErrorCode)
                .HasDefaultValue(0)
                .HasColumnName("error_code");
            entity.Property(e => e.ErrorMessage).HasColumnName("error_message");
            entity.Property(e => e.Inserted)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("inserted");
            entity.Property(e => e.IpAddress).HasColumnName("ip_address");
            entity.Property(e => e.RequestCertificateData).HasColumnName("request_certificate_data");
            entity.Property(e => e.RequestCertificateThumbprint).HasColumnName("request_certificate_thumbprint");
            entity.Property(e => e.RequestDateTime)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("request_date_time");
            entity.Property(e => e.ResponseDateTime)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("response_date_time");
            entity.Property(e => e.ResponseGuid).HasColumnName("response_guid");
            entity.Property(e => e.ResponseSignedData).HasColumnName("response_signed_data");
            entity.Property(e => e.ResponseXml)
                .HasColumnType("xml")
                .HasColumnName("response_xml");
            entity.Property(e => e.TempGuid).HasColumnName("temp_guid");
            entity.Property(e => e.ValidationDateTime)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("validation_date_time");

            entity.HasOne(d => d.Abonent).WithMany(p => p.TeDlanswers)
                .HasForeignKey(d => d.AbonentId)
                .HasConstraintName("te_dlanswers_abonents_fk");

            entity.HasOne(d => d.ErrorCodeNavigation).WithMany(p => p.TeDlanswers)
                .HasForeignKey(d => d.ErrorCode)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("te_dlanswers_error_codes_fk");
        });

        modelBuilder.Entity<TeDlput>(entity =>
        {
            entity.HasKey(e => e.KeyId).HasName("te_dlrequests_pk_1");

            entity.ToTable("te_dlputs", "qbchv3");

            entity.Property(e => e.KeyId)
                .HasDefaultValueSql("nextval('qbchv3.te_dlputs_key_id_seq1'::regclass)")
                .HasColumnName("key_id");
            entity.Property(e => e.AbonentId).HasColumnName("abonent_id");
            entity.Property(e => e.AddCommandsCount).HasColumnName("add_commands_count");
            entity.Property(e => e.DeleteCommandsCount).HasColumnName("delete_commands_count");
            entity.Property(e => e.ErrorCode)
                .HasDefaultValue(0)
                .HasColumnName("error_code");
            entity.Property(e => e.ErrorMessage).HasColumnName("error_message");
            entity.Property(e => e.Guid).HasColumnName("guid");
            entity.Property(e => e.Inserted)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("inserted");
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

            entity.HasOne(d => d.Abonent).WithMany(p => p.TeDlputs)
                .HasForeignKey(d => d.AbonentId)
                .HasConstraintName("te_dlputs_abonents_fk");
        });

        modelBuilder.Entity<TeDlputanswer>(entity =>
        {
            entity.HasKey(e => e.KeyId).HasName("te_dlputanswers_pk");

            entity.ToTable("te_dlputanswers", "qbchv3", tb => tb.HasComment("Таблица со всеми запросами dlputanswer которые пришли к нам"));

            entity.Property(e => e.KeyId)
                .HasDefaultValueSql("nextval('qbchv3.te_dlputanswers_key_id_seq1'::regclass)")
                .HasColumnName("key_id");
            entity.Property(e => e.AbonentId).HasColumnName("abonent_id");
            entity.Property(e => e.ErrorCode)
                .HasDefaultValue(0)
                .HasColumnName("error_code");
            entity.Property(e => e.ErrorMessage).HasColumnName("error_message");
            entity.Property(e => e.Guid).HasColumnName("guid");
            entity.Property(e => e.Inserted)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("inserted");
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

            entity.HasOne(d => d.Abonent).WithMany(p => p.TeDlputanswers)
                .HasForeignKey(d => d.AbonentId)
                .HasConstraintName("te_dlputanswers_abonents_fk");
        });

        modelBuilder.Entity<TeDlrequest>(entity =>
        {
            entity.HasKey(e => e.KeyId).HasName("te_dlrequests_pk");

            entity.ToTable("te_dlrequests", "qbchv3", tb => tb.HasComment("Запросы dlrequest которые поступили к нам. Здесь есть как запросы от других КБКИ так и от наших контрагентов.\r\nКод ответа 12 не отдается клиенту - является информационным."));

            entity.Property(e => e.KeyId)
                .HasDefaultValueSql("nextval('qbchv3.te_dlrequests_key_id_seq1'::regclass)")
                .HasColumnName("key_id");
            entity.Property(e => e.AbonentId).HasColumnName("abonent_id");
            entity.Property(e => e.ErrorCode)
                .HasDefaultValue(0)
                .HasColumnName("error_code");
            entity.Property(e => e.ErrorMessage).HasColumnName("error_message");
            entity.Property(e => e.InformationCode)
                .HasComment("Атрибут «КодСведений» заполняется одним из следующих значений:\r\n3 – абонент запрашивает сведения о среднемесячных платежах Субъекта, включая\r\nсведения о запрете (снятии запрета) на заключение договоров потребительского займа\r\n(кредита);\r\n6 – абонент запрашивает сведения о запрете (снятии запрета) на заключение договоров\r\nпотребительского займа (кредита);")
                .HasColumnName("information_code");
            entity.Property(e => e.Inserted)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("inserted");
            entity.Property(e => e.IpAddress).HasColumnName("ip_address");
            entity.Property(e => e.QbchTasksEndDateTime)
                .HasComment("Время выполнения задачи по сбору данных в БД или из других КБКИ")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("qbch_tasks_end_date_time");
            entity.Property(e => e.QbchTasksResultXml)
                .HasColumnType("xml")
                .HasColumnName("qbch_tasks_result_xml");
            entity.Property(e => e.RequestCertificateData)
                .HasComment("Массив байт канального сертификата")
                .HasColumnName("request_certificate_data");
            entity.Property(e => e.RequestCertificateThumbprint)
                .HasComment("Отпечаток сертификата запроса (Канальный	 сертификат)")
                .HasColumnName("request_certificate_thumbprint");
            entity.Property(e => e.RequestDateTime)
                .HasComment("Дата и время поступления запроса dlrequest")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("request_date_time");
            entity.Property(e => e.RequestId)
                .HasComment("id запроса")
                .HasColumnName("request_id");
            entity.Property(e => e.RequestMode)
                .HasComment("Атрибут «РежимЗапроса» заполняется одним из следующих значений:\r\n1 – абонент запрашивает сведения в отношении одного субъекта;\r\n2 – абонент запрашивает сведения в отношении нескольких субъектов (далее – пакет)")
                .HasColumnName("request_mode");
            entity.Property(e => e.RequestSignedData).HasColumnName("request_signed_data");
            entity.Property(e => e.RequestType)
                .HasComment("Атрибут «ТипЗапроса» заполняется одним из следующих значений:\r\n1 – абонент запрашивает сведения только у запрашиваемого КБКИ;\r\n2 – абонент запрашивает сведения всех КБКИ путем обращения в одно КБКИ (режим\r\n«одно окно»).")
                .HasColumnName("request_type");
            entity.Property(e => e.RequestXml)
                .HasColumnType("xml")
                .HasColumnName("request_xml");
            entity.Property(e => e.ResponseDateTime)
                .HasComment("Дата и время ответа на dlrequest")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("response_date_time");
            entity.Property(e => e.ResponseGuid)
                .HasComment("Guid по которому будет сформирован ответ. Генерируется автоматически при получении запроса")
                .HasColumnName("response_guid");
            entity.Property(e => e.ResponseSignedData).HasColumnName("response_signed_data");
            entity.Property(e => e.ResponseXml)
                .HasColumnType("xml")
                .HasColumnName("response_xml");
            entity.Property(e => e.ValidationDateTime)
                .HasComment("Время окончания валидации")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("validation_date_time");

            entity.HasOne(d => d.Abonent).WithMany(p => p.TeDlrequests)
                .HasForeignKey(d => d.AbonentId)
                .HasConstraintName("te_dlrequests_abonents_fk");

            entity.HasOne(d => d.ErrorCodeNavigation).WithMany(p => p.TeDlrequests)
                .HasForeignKey(d => d.ErrorCode)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("te_dlrequests_error_codes_fk");

            entity.HasOne(d => d.InformationCodeNavigation).WithMany(p => p.TeDlrequests)
                .HasForeignKey(d => d.InformationCode)
                .HasConstraintName("te_dlrequests_information_codes_fk");

            entity.HasOne(d => d.RequestModeNavigation).WithMany(p => p.TeDlrequests)
                .HasForeignKey(d => d.RequestMode)
                .HasConstraintName("te_dlrequests_request_modes_fk");

            entity.HasOne(d => d.RequestTypeNavigation).WithMany(p => p.TeDlrequests)
                .HasForeignKey(d => d.RequestType)
                .HasConstraintName("te_dlrequests_request_types_fk");
        });

        modelBuilder.Entity<TeQbchDlanswer>(entity =>
        {
            entity.HasKey(e => e.KeyId).HasName("te_qbch_dlanswers_pk");

            entity.ToTable("te_qbch_dlanswers", "qbchv3");

            entity.Property(e => e.KeyId).HasColumnName("key_id");
            entity.Property(e => e.ErrorCode)
                .HasDefaultValue(0)
                .HasColumnName("error_code");
            entity.Property(e => e.ErrorMessage).HasColumnName("error_message");
            entity.Property(e => e.HttpResponseCode).HasColumnName("http_response_code");
            entity.Property(e => e.Inserted)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("inserted");
            entity.Property(e => e.QbchTaskId).HasColumnName("qbch_task_id");
            entity.Property(e => e.RequestDateTime)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("request_date_time");
            entity.Property(e => e.ResponseData).HasColumnName("response_data");
            entity.Property(e => e.ResponseDateTime)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("response_date_time");
            entity.Property(e => e.ResponseXml)
                .HasColumnType("xml")
                .HasColumnName("response_xml");

            entity.HasOne(d => d.ErrorCodeNavigation).WithMany(p => p.TeQbchDlanswers)
                .HasForeignKey(d => d.ErrorCode)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("te_qbch_dlanswers_error_codes_fk");

            entity.HasOne(d => d.QbchTask).WithMany(p => p.TeQbchDlanswers)
                .HasForeignKey(d => d.QbchTaskId)
                .HasConstraintName("te_qbch_dlanswers_qbch_tasks_fk");
        });

        modelBuilder.Entity<TeQbchDlrequest>(entity =>
        {
            entity.HasKey(e => e.KeyId).HasName("te_qbch_dlrequests_pk");

            entity.ToTable("te_qbch_dlrequests", "qbchv3");

            entity.Property(e => e.KeyId)
                .HasDefaultValueSql("nextval('qbchv3.te_qbch_dlrequests_key_id_seq1'::regclass)")
                .HasColumnName("key_id");
            entity.Property(e => e.ErrorCode)
                .HasDefaultValue(0)
                .HasColumnName("error_code");
            entity.Property(e => e.ErrorMessage).HasColumnName("error_message");
            entity.Property(e => e.HttpResponseCode).HasColumnName("http_response_code");
            entity.Property(e => e.Inserted)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("inserted");
            entity.Property(e => e.QbchTaskId).HasColumnName("qbch_task_id");
            entity.Property(e => e.RequestData).HasColumnName("request_data");
            entity.Property(e => e.RequestDateTime)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("request_date_time");
            entity.Property(e => e.RequestXml)
                .HasColumnType("xml")
                .HasColumnName("request_xml");
            entity.Property(e => e.ResponseData).HasColumnName("response_data");
            entity.Property(e => e.ResponseDateTime)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("response_date_time");
            entity.Property(e => e.ResponseXml)
                .HasColumnType("xml")
                .HasColumnName("response_xml");

            entity.HasOne(d => d.ErrorCodeNavigation).WithMany(p => p.TeQbchDlrequests)
                .HasForeignKey(d => d.ErrorCode)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("te_qbch_dlrequests_error_codes_fk");

            entity.HasOne(d => d.QbchTask).WithMany(p => p.TeQbchDlrequests)
                .HasForeignKey(d => d.QbchTaskId)
                .HasConstraintName("te_qbch_dlrequests_qbch_tasks_fk");
        });

        modelBuilder.Entity<TeQbchTask>(entity =>
        {
            entity.HasKey(e => e.KeyId).HasName("te_qbch_tasks_pk");

            entity.ToTable("te_qbch_tasks", "qbchv3", tb => tb.HasComment("Задача в рамках которой отправляются запросы во внешние КБКИ. (Сбор данных из нашей БД так же приравнивается к внешней КБКИ)"));

            entity.Property(e => e.KeyId).HasColumnName("key_id");
            entity.Property(e => e.Inserted)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("inserted");
            entity.Property(e => e.QbchCorrespondentId).HasColumnName("qbch_correspondent_id");
            entity.Property(e => e.ReqId).HasColumnName("req_id");
            entity.Property(e => e.ResponseId).HasColumnName("response_id");
            entity.Property(e => e.TaskEndDateTime)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("task_end_date_time");
            entity.Property(e => e.TaskResultXml)
                .HasColumnType("xml")
                .HasColumnName("task_result_xml");
            entity.Property(e => e.TaskStartDateTime)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("task_start_date_time");

            entity.HasOne(d => d.QbchCorrespondent).WithMany(p => p.TeQbchTasks)
                .HasForeignKey(d => d.QbchCorrespondentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("te_qbch_tasks_abonents_fk");

            entity.HasOne(d => d.Req).WithMany(p => p.TeQbchTasks)
                .HasForeignKey(d => d.ReqId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("te_qbch_tasks_request_fk");
        });

        modelBuilder.Entity<TeRequest>(entity =>
        {
            entity.HasKey(e => e.KeyId).HasName("te_qbch_requests_pk");

            entity.ToTable("te_requests", "qbchv3", tb => tb.HasComment("Запросы поступившие в dlrequest будут разложены по одному в данную таблицу, вне зависимости от типа запроса (пакетный/одиночный)."));

            entity.Property(e => e.KeyId)
                .HasDefaultValueSql("nextval('qbchv3.te_requests_key_id_seq1'::regclass)")
                .HasColumnName("key_id");
            entity.Property(e => e.DlrequestId).HasColumnName("dlrequest_id");
            entity.Property(e => e.ErrorCode)
                .HasDefaultValue(0)
                .HasColumnName("error_code");
            entity.Property(e => e.ErrorMessage).HasColumnName("error_message");
            entity.Property(e => e.Inserted)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("inserted");
            //entity.Property(e => e.ObligationAmount).HasColumnName("obligation_amount");
            //entity.Property(e => e.ObligationAmountCurrency).HasColumnName("obligation_amount_currency");
            entity.Property(e => e.OrderNum).HasColumnName("order_num");
            entity.Property(e => e.RequestXml)
                .HasColumnType("xml")
                .HasColumnName("request_xml");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Dlrequest).WithMany(p => p.TeRequests)
                .HasForeignKey(d => d.DlrequestId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("te_qbch_requests_dlrequest_fk");

            entity.HasOne(d => d.ErrorCodeNavigation).WithMany(p => p.TeRequests)
                .HasForeignKey(d => d.ErrorCode)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("te_requests_error_codes_fk");

            entity.HasOne(d => d.User).WithMany(p => p.TeRequests)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("te_requests_users_fk");
        });

        modelBuilder.Entity<TeResponse>(entity =>
        {
            entity.HasKey(e => e.KeyId).HasName("te_responses_pk");

            entity.ToTable("te_responses", "qbchv3", tb => tb.HasComment("Ответы собранные в qbch_tasks будут разложены в данную таблицу вне зависимости от типа - пакетный запрос или одиночный."));

            entity.Property(e => e.KeyId)
                .HasDefaultValueSql("nextval('qbchv3.te_responses_key_id_seq1'::regclass)")
                .HasColumnName("key_id");
            entity.Property(e => e.AmpResponseType).HasColumnName("amp_response_type");
            entity.Property(e => e.ErrorCode)
                .HasDefaultValue(0)
                .HasColumnName("error_code");
            entity.Property(e => e.ErrorMessage).HasColumnName("error_message");
            entity.Property(e => e.Inserted)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("inserted");
            entity.Property(e => e.OrderNum).HasColumnName("order_num");
            entity.Property(e => e.QbchTaskId).HasColumnName("qbch_task_id");
            entity.Property(e => e.ResponseXml)
                .HasColumnType("xml")
                .HasColumnName("response_xml");
            entity.Property(e => e.SpResponseType).HasColumnName("sp_response_type");

            entity.HasOne(d => d.AmpResponseTypeNavigation).WithMany(p => p.TeResponses)
                .HasForeignKey(d => d.AmpResponseType)
                .HasConstraintName("te_responses_amp_response_type_fk");

            entity.HasOne(d => d.QbchTask).WithMany(p => p.TeResponses)
                .HasForeignKey(d => d.QbchTaskId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("te_responses_qbch_dlrequests_fk");

            entity.HasOne(d => d.SpResponseTypeNavigation).WithMany(p => p.TeResponses)
                .HasForeignKey(d => d.SpResponseType)
                .HasConstraintName("te_responses_sp_response_type_fk");
        });

        modelBuilder.Entity<TeSubject>(entity =>
        {
            entity.HasKey(e => e.KeyId).HasName("te_subjects_pkey");

            entity.ToTable("te_subjects", "qbchv3", tb => tb.HasComment("Субъект в запросе"));

            entity.Property(e => e.KeyId)
                .HasDefaultValueSql("nextval('qbchv3.te_subjects_key_id_seq1'::regclass)")
                .HasColumnName("key_id");
            entity.Property(e => e.BirthDay).HasColumnName("birth_day");
            entity.Property(e => e.Inn).HasColumnName("inn");
            entity.Property(e => e.InnChecked).HasColumnName("inn_checked");
            entity.Property(e => e.InnForeign).HasColumnName("inn_foreign");
            entity.Property(e => e.Inserted)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("inserted");
            entity.Property(e => e.Psrn).HasColumnName("psrn");
            entity.Property(e => e.RequestId).HasColumnName("request_id");
            entity.Property(e => e.Snils).HasColumnName("snils");

            entity.HasOne(d => d.Request).WithMany(p => p.TeSubjects)
                .HasForeignKey(d => d.RequestId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("te_subjects_request_fk");
        });

        modelBuilder.Entity<TeSubjectsDocument>(entity =>
        {
            entity.HasKey(e => e.KeyId).HasName("te_subjects_documents_pkey");

            entity.ToTable("te_subjects_documents", "qbchv3");

            entity.Property(e => e.KeyId)
                .HasDefaultValueSql("nextval('qbchv3.te_subjects_documents_key_id_seq1'::regclass)")
                .HasColumnName("key_id");
            entity.Property(e => e.CountryCode).HasColumnName("country_code");
            entity.Property(e => e.DocDateIssue).HasColumnName("doc_date_issue");
            entity.Property(e => e.DocNumber).HasColumnName("doc_number");
            entity.Property(e => e.DocSeries).HasColumnName("doc_series");
            entity.Property(e => e.DocTypeId).HasColumnName("doc_type_id");
            entity.Property(e => e.Inserted)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("inserted");
            entity.Property(e => e.SubjectId).HasColumnName("subject_id");

            entity.HasOne(d => d.DocType).WithMany(p => p.TeSubjectsDocuments)
                .HasForeignKey(d => d.DocTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("te_subjects_documents_document_types_fk");

            entity.HasOne(d => d.Subject).WithMany(p => p.TeSubjectsDocuments)
                .HasForeignKey(d => d.SubjectId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("te_subjects_documents_subjects_fk");
        });

        modelBuilder.Entity<TeSubjectsFullName>(entity =>
        {
            entity.HasKey(e => e.KeyId).HasName("te_subjects_full_name_pkey");

            entity.ToTable("te_subjects_full_name", "qbchv3");

            entity.Property(e => e.KeyId)
                .HasDefaultValueSql("nextval('qbchv3.te_subjects_full_name_key_id_seq1'::regclass)")
                .HasColumnName("key_id");
            entity.Property(e => e.FirstName).HasColumnName("first_name");
            entity.Property(e => e.Inserted)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("inserted");
            entity.Property(e => e.LastName).HasColumnName("last_name");
            entity.Property(e => e.MiddleName).HasColumnName("middle_name");
            entity.Property(e => e.SubjectId).HasColumnName("subject_id");

            entity.HasOne(d => d.Subject).WithMany(p => p.TeSubjectsFullNames)
                .HasForeignKey(d => d.SubjectId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("te_subjects_full_name_fk");
        });

        modelBuilder.Entity<TrAbonent>(entity =>
        {
            entity.HasKey(e => e.KeyId).HasName("tr_abonents_pkey");

            entity.ToTable("tr_abonents", "qbchv3", tb => tb.HasComment("Список абонентов"));

            entity.Property(e => e.KeyId)
                .HasDefaultValueSql("nextval('qbchv3.tr_abonents_key_id_seq1'::regclass)")
                .HasColumnName("key_id");
            entity.Property(e => e.Email).HasColumnName("email");
            entity.Property(e => e.FullName).HasColumnName("full_name");
            entity.Property(e => e.Inn).HasColumnName("inn");
            entity.Property(e => e.Inserted)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("inserted");
            entity.Property(e => e.Ogrn).HasColumnName("ogrn");
            entity.Property(e => e.ShortName).HasColumnName("short_name");
            entity.Property(e => e.UserType).HasColumnName("user_type");

            entity.HasOne(d => d.UserTypeNavigation).WithMany(p => p.TrAbonents)
                .HasForeignKey(d => d.UserType)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("tr_abonents_user_types_fk");
        });

        modelBuilder.Entity<TrAbonentCertificate>(entity =>
        {
            entity.HasKey(e => e.KeyId).HasName("tr_abonent_certificates_pkey");

            entity.ToTable("tr_abonent_certificates", "qbchv3", tb => tb.HasComment("Список сертификатов связанных с абонентом"));

            entity.Property(e => e.KeyId)
                .HasDefaultValueSql("nextval('qbchv3.tr_abonent_certificates_key_id_seq1'::regclass)")
                .HasColumnName("key_id");
            entity.Property(e => e.AbonentId).HasColumnName("abonent_id");
            entity.Property(e => e.ExpirationDate)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("expiration_date");
            entity.Property(e => e.Inserted)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("inserted");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(false)
                .HasColumnName("is_active");
            entity.Property(e => e.Thumbprint).HasColumnName("thumbprint");

            entity.HasOne(d => d.Abonent).WithMany(p => p.TrAbonentCertificates)
                .HasForeignKey(d => d.AbonentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("tr_abonent_certificates_abonents_fk");
        });

        modelBuilder.Entity<TrAmpResponseType>(entity =>
        {
            entity.HasKey(e => e.KeyId).HasName("tr_qbch_response_types_pk");

            entity.ToTable("tr_amp_response_types", "qbchv3", tb => tb.HasComment("Тип ответа. Результативный/не результативный"));

            entity.Property(e => e.KeyId)
                .ValueGeneratedNever()
                .HasColumnName("key_id");
            entity.Property(e => e.ResponseData).HasColumnName("response_data");
            entity.Property(e => e.WhoseData).HasColumnName("whose_data");
        });

        modelBuilder.Entity<TrDlrequestType>(entity =>
        {
            entity.HasKey(e => e.KeyId).HasName("tr_dlrequest_types_pk");

            entity.ToTable("tr_dlrequest_types", "qbchv3", tb => tb.HasComment("Тип запроса\r\n1 = Только наши данные\r\n2 = Одно окно (Нужно собирать со всех КБКИ)"));

            entity.Property(e => e.KeyId)
                .ValueGeneratedNever()
                .HasColumnName("key_id");
            entity.Property(e => e.Description).HasColumnName("description");
        });

        modelBuilder.Entity<TrDocumentType>(entity =>
        {
            entity.HasKey(e => e.KeyId).HasName("tr_document_types_pk");

            entity.ToTable("tr_document_types", "qbchv3", tb => tb.HasComment("Типы документов"));

            entity.Property(e => e.KeyId).HasColumnName("key_id");
            entity.Property(e => e.DocDescription).HasColumnName("doc_description");
        });

        modelBuilder.Entity<TrErrorCode>(entity =>
        {
            entity.HasKey(e => e.KeyId).HasName("tr_error_codes_pkey");

            entity.ToTable("tr_error_codes", "qbchv3", tb => tb.HasComment("Коды ошибок согласно документации\r\n+ добавленные коды 500 и 0"));

            entity.Property(e => e.KeyId)
                .ValueGeneratedNever()
                .HasColumnName("key_id");
            entity.Property(e => e.Comments).HasColumnName("comments");
            entity.Property(e => e.Description).HasColumnName("description");
        });

        modelBuilder.Entity<TrInformationCode>(entity =>
        {
            entity.HasKey(e => e.KeyId).HasName("tr_information_codes_pk");

            entity.ToTable("tr_information_codes", "qbchv3");

            entity.Property(e => e.KeyId)
                .ValueGeneratedNever()
                .HasColumnName("key_id");
            entity.Property(e => e.Description).HasColumnName("description");
        });

        modelBuilder.Entity<TrRequestMode>(entity =>
        {
            entity.HasKey(e => e.KeyId).HasName("tr_request_modes_pk");

            entity.ToTable("tr_request_modes", "qbchv3");

            entity.Property(e => e.KeyId)
                .ValueGeneratedNever()
                .HasColumnName("key_id");
            entity.Property(e => e.Description).HasColumnName("description");
        });

        modelBuilder.Entity<TrService>(entity =>
        {
            entity.HasKey(e => e.KeyId).HasName("tr_services_pkey");

            entity.ToTable("tr_services", "qbchv3");

            entity.Property(e => e.KeyId)
                .ValueGeneratedNever()
                .HasColumnName("key_id");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.ServiceName).HasColumnName("service_name");
        });

        modelBuilder.Entity<TrSpResponseType>(entity =>
        {
            entity.HasKey(e => e.KeyId).HasName("newtable_pk");

            entity.ToTable("tr_sp_response_type", "qbchv3");

            entity.Property(e => e.KeyId)
                .ValueGeneratedNever()
                .HasColumnName("key_id");
            entity.Property(e => e.ResponseData).HasColumnName("response_data");
        });

        modelBuilder.Entity<TrUserType>(entity =>
        {
            entity.HasKey(e => e.KeyId).HasName("tr_user_types_pkey");

            entity.ToTable("tr_user_types", "qbchv3");

            entity.Property(e => e.KeyId)
                .ValueGeneratedNever()
                .HasColumnName("key_id");
            entity.Property(e => e.Description).HasColumnName("description");
        });

        modelBuilder.Entity<TrUserTypeCode>(entity =>
        {
            entity.HasKey(e => e.Code).HasName("tr_user_type_code_pkey");

            entity.ToTable("tr_user_type_code", "qbchv3");

            entity.Property(e => e.Code)
                .ValueGeneratedNever()
                .HasColumnName("code");
            entity.Property(e => e.Description).HasColumnName("description");
        });
        modelBuilder.Entity<TeConsentPurpose>(entity =>
        {
            entity.HasKey(e => e.KeyId).HasName("te_consent_purposes_pk");

            entity.ToTable("te_consent_purposes", "qbchv3");

            entity.Property(e => e.KeyId)
                .HasDefaultValueSql("nextval('qbchv3.te_consent_purposes_id_seq'::regclass)")
                .HasColumnName("id");
            entity.Property(e => e.PurposeId).HasColumnName("purpose_id");
            entity.Property(e => e.RequestId).HasColumnName("request_id");

            //entity.HasOne(d => d.Request).WithMany(p => p.TeConsentPurposes)
            //    .HasForeignKey(d => d.RequestId)
            //    .OnDelete(DeleteBehavior.ClientSetNull)
            //    .HasConstraintName("te_consent_purposes_request_fk");
        });

        modelBuilder.Entity<TeRequestPurpose>(entity =>
        {
            entity.HasKey(e => e.KeyId).HasName("te_request_purposes_pk");

            entity.ToTable("te_request_purposes", "qbchv3");

            entity.Property(e => e.KeyId)
                .HasDefaultValueSql("nextval('qbchv3.te_request_purposes_id_seq'::regclass)")
                .HasColumnName("id");
            entity.Property(e => e.PurposeId).HasColumnName("purpose_id");
            entity.Property(e => e.RequestId).HasColumnName("request_id");

            //entity.HasOne(d => d.Request).WithMany(p => p.TeRequestPurposes)
            //    .HasForeignKey(d => d.RequestId)
            //    .OnDelete(DeleteBehavior.ClientSetNull)
            //    .HasConstraintName("te_request_purposes_request_fk");
        });

        modelBuilder.HasSequence("td_permissions_key_id_seq", "qbchv3").HasMax(2147483647L);
        modelBuilder.HasSequence("td_users_key_id_seq", "qbchv3");
        modelBuilder.HasSequence("te_consent_purposes_id_seq", "qbchv3");
        modelBuilder.HasSequence("te_request_purposes_id_seq", "qbchv3");
        modelBuilder.HasSequence("te_cert_manage_key_id_seq", "qbchv3");
        modelBuilder.HasSequence("te_dlanswers_key_id_seq", "qbchv3");
        modelBuilder.HasSequence("te_dlputanswers_key_id_seq", "qbchv3");
        modelBuilder.HasSequence("te_dlputs_key_id_seq", "qbchv3");
        modelBuilder.HasSequence("te_dlrequests_key_id_seq", "qbchv3");
        modelBuilder.HasSequence("te_qbch_dlanswers_id_seq", "qbchv3");
        modelBuilder.HasSequence("te_qbch_dlrequests_id_seq", "qbchv3");
        modelBuilder.HasSequence("te_qbch_dlrequests_key_id_seq", "qbchv3");
        modelBuilder.HasSequence("te_requests_key_id_seq", "qbchv3");
        modelBuilder.HasSequence("te_responses_key_id_seq", "qbchv3");
        modelBuilder.HasSequence("te_subjects_documents_key_id_seq", "qbchv3");
        modelBuilder.HasSequence("te_subjects_full_name_key_id_seq", "qbchv3");
        modelBuilder.HasSequence("te_subjects_key_id_seq", "qbchv3");
        modelBuilder.HasSequence("tr_abonent_certificates_key_id_seq", "qbchv3").HasMax(2147483647L);
        modelBuilder.HasSequence("tr_abonents_key_id_seq", "qbchv3").HasMax(2147483647L);

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
