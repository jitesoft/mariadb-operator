using k8s.Operators;

namespace Jitesoft.MariaDBOperator.Resources;

// ReSharper disable once StringLiteralTypo
[CustomResourceDefinition("jitesoft.tech", "v1alpha1", "mariadbs")]
public class MariaDB : CustomResource<MariaDBSpec, MariaDBStatus>
{
}
