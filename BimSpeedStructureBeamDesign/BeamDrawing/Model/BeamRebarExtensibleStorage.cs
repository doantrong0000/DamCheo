using Autodesk.Revit.DB;
using Autodesk.Revit.DB.ExtensibleStorage;

namespace BimSpeedStructureBeamDesign.BeamDrawing.Model
{
   public class BeamRebarExtensibleStorage
   {
      public BeamRebarExtensibleStorage()
      {
      }

      /// <summary>
      /// Create a data structure, attach it to a wall,
      /// populate it with data, and retrieve the data
      /// back from the wall
      /// </summary>
      public void StoreDataInWall(Wall wall, XYZ dataToStore)
      {
         Transaction createSchemaAndStoreData
           = new Transaction(wall.Document, "tCreateAndStore");

         createSchemaAndStoreData.Start();
         SchemaBuilder schemaBuilder = new SchemaBuilder(
           new Guid("720080CB-DA99-40DC-9415-E53F280AA1F0"));

         // allow anyone to read the object
         schemaBuilder.SetReadAccessLevel(
           AccessLevel.Public);

         // restrict writing to this vendor only
         schemaBuilder.SetWriteAccessLevel(
           AccessLevel.Vendor);

         // required because of restricted write-access
         schemaBuilder.SetVendorId("ADSK");

         // create a field to store an XYZ
         FieldBuilder fieldBuilder = schemaBuilder
           .AddSimpleField("WireSpliceLocation",
           typeof(XYZ));
#if Version2017 || Version2018 || Version2019 || Version2020
         fieldBuilder.SetUnitType(UnitType.UT_Length);
#else
         fieldBuilder.SetSpec(SpecTypeId.Length);
#endif

         fieldBuilder.SetDocumentation("A stored "
           + "location value representing a wiring "
           + "splice in a wall.");

         schemaBuilder.SetSchemaName("WireSpliceLocation");

         Schema schema = schemaBuilder.Finish();

         Entity entity = new Entity(schema);

         Field fieldSpliceLocation = schema.GetField("WireSpliceLocation");
#if Version2017 || Version2018 || Version2019|| Version2020
         entity.Set<XYZ>(fieldSpliceLocation, dataToStore, 0);
#else
         entity.Set<XYZ>(fieldSpliceLocation, dataToStore, UnitTypeId.Meters);
#endif
         wall.SetEntity(entity);

         Entity retrievedEntity = wall.GetEntity(schema);
#if Version2017 || Version2018 || Version2019 || Version2020
         XYZ retrievedData = retrievedEntity.Get<XYZ>(schema.GetField("WireSpliceLocation"), 0);
#else
         XYZ retrievedData = retrievedEntity.Get<XYZ>(schema.GetField("WireSpliceLocation"), UnitTypeId.Meters);
#endif

         createSchemaAndStoreData.Commit();
      }
   }
}