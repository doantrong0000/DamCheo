using Autodesk.Revit.DB.ExtensibleStorage;
using Autodesk.Revit.DB.Structure;
using BimSpeedStructureBeamDesign.BeamRebar.Services;

namespace BimSpeedStructureBeamDesign.BeamRebar.Model.RebarModel
{
   public class RebarExtensibleStorage
   {
      public Rebar Rebar { get; set; }
      public string RebarLayer { get; set; }
      public string RebarType { get; set; }
      public Entity Entity { get; set; }

      public RebarExtensibleStorage(Rebar rebar)
      {
         //Get Data
      }

      public static void SetSchemaForMainBar(List<Rebar> rebars, MainRebar mainRebar)
      {
         return;
         var layer = 1.ToString();

         foreach (var rebar in rebars)
         {
            SetEntityForRebar(rebar, layer, mainRebar.IsTop ? Define.MainBarTop : Define.MainBarBot);
         }
      }

      public static void SetSchemaForAdditionalTopBar(List<Rebar> rebars, TopAdditionalBar bar)
      {
         var layer = bar.Layer.ToString();

         foreach (var rebar in rebars)
         {
            SetEntityForRebar(rebar, layer, Define.AddtitionalTopBar);
         }
      }

      public static void SetSchemaForAdditionalBotBar(List<Rebar> rebars, BottomAdditionalBar bar)
      {
         return;
         var layer = bar.Layer.ToString();

         foreach (var rebar in rebars)
         {
            SetEntityForRebar(rebar, layer, Define.AddtitionalBotBar);
         }
      }

      public static void SetEntityForRebar(Rebar rebar, string layer, string rebarType)
      {
         try
         {
            var schema = GetSchema(Define.RebarSchemaGuid);
            var entity = rebar.GetEntity(schema);
            if (entity.IsValidObject == false || entity.Schema == null)
            {
               entity = new Entity(schema);
            }
            Field fieldRebarLayer = schema.GetField(
                Define.StorageFieldRebarLayer);
            if (fieldRebarLayer != null)
            {
               entity.Set(fieldRebarLayer, layer);
            }
            Field fieldRebarType = schema.GetField(
                Define.StorageFieldRebarType);
            if (fieldRebarType != null)
            {
               entity.Set(fieldRebarType, rebarType);
            }
            rebar.SetEntity(entity);
         }
         catch (Exception)
         {
            //Console.WriteLine(e);
            //throw;
         }
      }

      public static void SetEntityForRebars(List<Rebar> rebars, string layer, string rebarType)
      {
         foreach (var rebar in rebars)
         {
            SetEntityForRebar(rebar, layer, rebarType);
         }
      }

      private static Schema GetSchema(Guid guid)
      {
         var schema = Schema.Lookup(guid);
         if (schema == null || schema.IsValidObject == false || schema.GUID == Guid.Empty)
         {
            var schemaBuilder = new SchemaBuilder(guid);
            schemaBuilder.SetSchemaName("RebarSchema");
            schemaBuilder.SetReadAccessLevel(
                AccessLevel.Public);

            // restrict writing to this vendor only
            schemaBuilder.SetWriteAccessLevel(
                AccessLevel.Vendor);
            // required because of restricted write-access
            schemaBuilder.SetVendorId("ADSK");

            // create a field to store an XYZ
            FieldBuilder fieldBuilder1 = schemaBuilder
                .AddSimpleField(Define.StorageFieldRebarLayer,
                    typeof(string));
            FieldBuilder fieldBuilder2 = schemaBuilder
                .AddSimpleField(Define.StorageFieldRebarType,
                    typeof(string));
            schema = schemaBuilder.Finish();
         }
         return schema;
      }
   }
}