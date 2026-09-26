using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using wirtualny_dziekanat.Data.Entities;

namespace wirtualny_dziekanat.Data
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
    {
        public DbSet<Student> Students => Set<Student>();
        public DbSet<Teacher> Teachers => Set<Teacher>();
        public DbSet<Kierunek> Kierunki => Set<Kierunek>();
        public DbSet<Grupa> Grupy => Set<Grupa>();
        public DbSet<Subject> Subjects => Set<Subject>();
        public DbSet<Enrollment> Enrollments => Set<Enrollment>();
        public DbSet<Grade> Grades => Set<Grade>();
        public DbSet<Schedule> Schedules => Set<Schedule>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Unikalny FK wymusza relację jeden-do-jednego profilu z kontem Identity.
            builder.Entity<Student>()
                .HasOne(student => student.ApplicationUser)
                .WithOne(user => user.Student)
                .HasForeignKey<Student>(student => student.ApplicationUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Teacher>()
                .HasOne(teacher => teacher.ApplicationUser)
                .WithOne(user => user.Teacher)
                .HasForeignKey<Teacher>(teacher => teacher.ApplicationUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Student>().HasIndex(student => student.ApplicationUserId).IsUnique();
            builder.Entity<Teacher>().HasIndex(teacher => teacher.ApplicationUserId).IsUnique();
            builder.Entity<Student>().HasIndex(student => student.NumerIndeksu).IsUnique();

            builder.Entity<Grupa>()
                .HasOne(group => group.Kierunek)
                .WithMany(direction => direction.Grupy)
                .HasForeignKey(group => group.KierunekId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Student>()
                .HasOne(student => student.Kierunek)
                .WithMany(direction => direction.Studenci)
                .HasForeignKey(student => student.KierunekId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Student>()
                .HasOne(student => student.Grupa)
                .WithMany(group => group.Studenci)
                .HasForeignKey(student => student.GrupaId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Subject>()
                .HasOne(subject => subject.Teacher)
                .WithMany(teacher => teacher.Subjects)
                .HasForeignKey(subject => subject.TeacherId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Enrollment>()
                .HasOne(enrollment => enrollment.Student)
                .WithMany(student => student.Enrollments)
                .HasForeignKey(enrollment => enrollment.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Enrollment>()
                .HasOne(enrollment => enrollment.Subject)
                .WithMany(subject => subject.Enrollments)
                .HasForeignKey(enrollment => enrollment.SubjectId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Enrollment>()
                .HasIndex(enrollment => new { enrollment.StudentId, enrollment.SubjectId })
                .IsUnique();

            builder.Entity<Grade>()
                .HasOne(grade => grade.Enrollment)
                .WithMany(enrollment => enrollment.Grades)
                .HasForeignKey(grade => grade.EnrollmentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Grade>().Property(grade => grade.WartoscOceny).HasPrecision(2, 1);

            builder.Entity<Schedule>()
                .HasOne(schedule => schedule.Grupa)
                .WithMany(group => group.Schedules)
                .HasForeignKey(schedule => schedule.GrupaId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Schedule>()
                .HasOne(schedule => schedule.Subject)
                .WithMany(subject => subject.Schedules)
                .HasForeignKey(schedule => schedule.SubjectId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
