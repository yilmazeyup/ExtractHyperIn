using System;
using System.Collections.Generic;
using HtmlAgilityPack;
using ScrapySharp.Extensions;
using ScrapySharp.Network;
using System.IO;
using System.Globalization;
using System.Text.RegularExpressions;
using Npgsql;

namespace Scraper
{
    class Program
    {
        static ScrapingBrowser _browser = new ScrapingBrowser();
        static void Main(string[] args)
        {
            //You can select the tab of the Price Charting site where you will extract the data from here.
            var mainPageLinks = GetMainPageLinks("https://www.hyperkin.com/full-product-list.html?product_list_limit=all");
        }

        static List<dynamic> GetMainPageLinks(string url)
        {

            var html = GetHtml(url);
            var connectionString = "Host=localhost;Username=postgres;Password=12345;Database=postgres";
            var links = html.CssSelect("li");

            //Here I am pulling the product information according to the css structure of the page and writing it to a list.
            List<dynamic> linksList = new List<dynamic>();



            int i = 0;
            //Parallel.ForEach(links ,link =>
            foreach (var link in links)           
                {
                Product product = new Product();
                    try
                    {
                        product.title = link.InnerHtml.Split("</a>")[1].Split(">")[14].Replace("\n", "").Trim().ToString();
                        

                    }
                    catch (Exception) { }
                    try
                    {
                        product.category = link.InnerHtml.Split("</strong>")[1].Split("</div>")[0].Split(">")[2].Replace("\n", "").Trim().ToString();

                    }
                    catch (Exception) { }
                    try
                    {
                        product.item = link.InnerHtml.Split("nbsp;")[1].Split("</div>")[0].Replace("\n", "").Trim().ToString();

                    }
                    catch (Exception) { }
                    try
                    {
                        product.price = Convert.ToDouble(link.InnerHtml.Split("amount=")[1].Split("data")[0].Replace("\"", ""));

                    }
                    catch (Exception) { }
                    try
                    {
                        var upcUrl = link.InnerHtml.Split("href=")[1].Split("class")[0].Replace("\"", "").Trim().ToString();
                        var upcHtml = GetDetailHtml(upcUrl);
                        product.upcCode = upcHtml.InnerHtml.Split("Code:")[1].Split("</s")[0].Trim().ToString();
                        product.image = upcHtml.InnerHtml.Split("og:image\" content=\"")[1].Split("\">")[0].Trim().ToString();
                    }
                    catch (Exception) { }
                    Console.WriteLine(product.title);
                i++;
                if (product.title != null )
                {
                    linksList.Add(product);
                   
                }else{}


                };


            //The data in the created list is being written to PostgreSQL.
            using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                NpgsqlCommand cmd = new NpgsqlCommand();
                cmd.Connection = connection;
                linksList.ForEach(x =>
                {

                    using (var cmd = new NpgsqlCommand(@"insert into hyperkin (title,category,item,price,upcCode,image ,createdon) 
                                values(:title,:category,:item,:price,:upcCode,:image,current_timestamp)
                                on conflict(upccode)
                                do update 
                                set
                                price =  :price  ", connection))
                    {
                        cmd.Parameters.AddWithValue("title", x.title == null ? "" : x.title.ToString());
                        cmd.Parameters.AddWithValue("category", x.category == null ? "" : x.category.ToString());
                        cmd.Parameters.AddWithValue("item", x.item == null ? "" : x.item.ToString());
                        cmd.Parameters.AddWithValue("price", x.price == null ? 0 : Convert.ToDouble(x.price));
                        cmd.Parameters.AddWithValue("upcCode", x.upcCode == "" || x.upcCode == null ? "" : x.upcCode.ToString());
                        cmd.Parameters.AddWithValue("image", x.image == null ? "" : x.image?.ToString());
                        cmd.ExecuteNonQuery();

                    }
                });
                cmd.Dispose();
                connection.Close();
            };
            //Finally, I give the list as output. Anyone can output the data in csv, xls or any other format they want.
            return linksList;
        }

        static HtmlNode GetHtml(string url)
        {
            
            try
            {

                WebPage webpage = _browser.NavigateToPage(new Uri(url));
                return webpage.Html;
            }
            catch
            {

                WebPage webpage = _browser.NavigateToPage(new Uri(url));
                return webpage.Html;
            }
        }

        static HtmlNode GetDetailHtml(string upcUrl)
        {
            WebPage webpage = _browser.NavigateToPage(new Uri(upcUrl));
            return webpage.Html;
        }



        public class Product
        {
            public string? title { get; set; }
            public string? image { get; set; }
            public string? category { get; set; }
            public string? item { get; set; }
            public double price { get; set; }
            public string? upcCode { get; set; }

        }
    }
}