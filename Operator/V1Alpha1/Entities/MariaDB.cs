using k8s.Models;
using KubeOps.Operator.Entities;

namespace Jitesoft.MariaDBOperator.V1Alpha1.Entities;

// ReSharper disable once StringLiteralTypo
[KubernetesEntity(
    ApiVersion = "v1alpha1",
    Group = "jitesoft.tech",
    Kind = "MariaDB",
    PluralName = "mariadbs")
]
public class MariaDB : CustomKubernetesEntity<MariaDBSpec, MariaDBStatus>
{ }
