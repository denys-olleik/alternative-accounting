using System;

namespace Accounting.CustomAttributes
{
  [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
  public class AllowWithoutOrganizationIdAttribute : Attribute
  {
  }
}