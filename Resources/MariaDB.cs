using k8s.Models;
using k8s.Operators;

namespace mariadb_operator.Resources;

[CustomResourceDefinition("jitesoft.tech", "v1", "Mariadbs")]
public class MariaDB : CustomResource<MariaDB.MariaDBSpec, MariaDB.MariaDBStatus>
{
    public class MariaDBSpec
    {
        public string Image { get; set; } = "mariadb:10.7";
        public int Port { get; set; } = 3306;
        public V1ResourceRequirements? Resources { get; set; }
        public string DbUser { get; set; } = null!;
        public string DbName { get; set; } = null!;
        public string DbPasswordSecretName { get; set; } = null!;
        public string DbPasswordSecretKey { get; set; } = "password";
    }

    public class MariaDBStatus {}

}
