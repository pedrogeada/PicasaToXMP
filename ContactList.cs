using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace PicasaToXMP
{
    public class Contact
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public DateTime ModifiedTime { get; set; }
        public int LocalContact { get; set; }
    }

    public class ContactList
    {
        public List<Contact> Contacts = new List<Contact>();

        public void ReadContactsFromFile(string filePath)
        {
            try
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(filePath);

                XmlNodeList? contactNodes = xmlDoc.SelectNodes("/contacts/contact");
                if (contactNodes == null)
                    return;

                foreach (XmlNode contactNode in contactNodes)
                {
                    string? id = contactNode.Attributes?["id"]?.Value;
                    string? name = contactNode.Attributes?["name"]?.Value;
                    DateTime modifiedTime = DateTime.Parse(contactNode.Attributes?["modified_time"]?.Value ?? "1970-01-01");
                    int localContact = int.Parse(contactNode.Attributes?["local_contact"]?.Value ?? "1");

                    // Create a new Contact object
                    Contact contact = new Contact
                    {
                        Id = id,
                        Name = name,
                        ModifiedTime = modifiedTime,
                        LocalContact = localContact
                    };

                    // Add the contact to the list
                    Contacts.Add(contact);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading contacts file: {ex.Message}");
            }
        }

        public void WriteContacts()
        {
            foreach(var contact in Contacts)
            {
                Console.WriteLine(contact.Id + ":" + contact.Name);
            }
        }

        public string GetContactId(string name)
        {
            Contact? c = Contacts.Find(contact => contact.Name == name);
            if (c != null)
            {
                return c.Id;
            }
            else
                return "";
        }

        public string GetContactName(string id)
        {
            Contact? c = Contacts.Find(contact => contact.Id == id);
            if (c != null)
            {
                return c.Name;
            }
            else
                return "";
        }
    }
}
