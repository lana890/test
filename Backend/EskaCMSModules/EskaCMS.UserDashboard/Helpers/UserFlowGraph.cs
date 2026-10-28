using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.UserDashboard.Helpers
{
    public class UserFlowGraph
    {

        public static readonly string OutSource = "OutSource";
        public readonly long StartingDepth = 0;

        private GraphNode Root = new GraphNode()
        {
            Slug = UserFlowGraph.OutSource
        };

        public GraphNode GetGraph()
        {
            return Root;
        }



        public void InsertNavigation(string ToSlug)
        {

            var ToNode = SearchForGraphNode(ToSlug, this.Root, this.StartingDepth);


            if (ToNode == null)
            {
                ToNode = AddNewNode(this.Root, ToSlug);
            }

            AddNewGraphLink(this.Root, ToNode);

        }


        public void InsertNavigation(string FromSlug, string ToSlug)
        {
            var FromNode = SearchForGraphNode(FromSlug,this.Root, this.StartingDepth);
            if (FromNode == null)
            {
                FromNode = AddNewNode(FromSlug);
            }

            if (string.IsNullOrWhiteSpace(ToSlug))
            {
                return;
            }

            var ToNode = SearchForGraphNode(ToSlug, this.Root, this.StartingDepth);
            

            if (ToNode == null)
            {
                ToNode = AddNewNode(FromNode,ToSlug);
            }

            AddNewGraphLink(FromNode, ToNode);

        }



        // overload
        // use this when there is no previous slug 
        private GraphNode AddNewNode(string Slug) 
        {
            GraphNode NewNode = new GraphNode()
            {
                Slug=Slug,
            };

            var GraphLink = new GraphLink()
            {
                FromNode = this.Root,
                ToNode = NewNode,
                NumberOfUsers = 1
            };
            this.Root.NextNavigations.Add(GraphLink);

            return NewNode;

        }




        // overload
        // use this when you have previous slug 
        private GraphNode AddNewNode(GraphNode FromNode, string ToSlug) 
        {
            GraphNode NewNode = new GraphNode()
            {
                Slug = ToSlug,
            };

            var GraphLink = new GraphLink()
            {
                FromNode = FromNode,
                ToNode = NewNode,
                NumberOfUsers = 1
            };
            this.Root.NextNavigations.Add(GraphLink);

            return NewNode;

        }



        private GraphLink AddNewGraphLink(GraphNode FromNode, GraphNode ToNode)
        {
            var Link = SearchForGraphLink(FromNode, ToNode);
            if (Link == null)
            {
                Link = new GraphLink()
                {
                    FromNode = FromNode,
                    ToNode = ToNode,
                    NumberOfUsers = 1
                };
                FromNode.NextNavigations.Add(Link);
            }
            else
            {
                Link.NumberOfUsers++;
            }
            

            return Link;

        }



        private GraphNode SearchForGraphNode(string TargetSlug, GraphNode CurrentNode,long Depth)
        {
            if (Depth == StartingDepth)
            {
                ResetVisitState(CurrentNode);
            }


            if (CurrentNode.Slug.ToLower() == TargetSlug.ToLower())
            {
                return CurrentNode;
            }
            else if (CurrentNode.NextNavigations == null || CurrentNode.NextNavigations.Count == 0)
            {
                return null;
            }
            else
            {
                foreach (var Navigationlink in CurrentNode.NextNavigations)
                {
                    if (Navigationlink.IsVisited)
                    {
                        continue;
                    }

                    Navigationlink.IsVisited = true; 
                    
                    var SearchResult = SearchForGraphNode(TargetSlug, Navigationlink.ToNode,Depth +1);
                    if (SearchResult != null)
                    {
                        return SearchResult;
                    }

                }
            }

           
            return null;

        }






        private GraphLink SearchForGraphLink(GraphNode FromNode, GraphNode ToNode)
        {
             
            return FromNode.NextNavigations.Where(n => n.ToNode == ToNode).FirstOrDefault();

        }


        private bool ResetVisitState(GraphNode CurrentNode)
        {
            foreach (var Navigationlink in CurrentNode.NextNavigations)
            {
                if (!Navigationlink.IsVisited)
                {
                    continue;
                }

                Navigationlink.IsVisited = false;

                ResetVisitState(Navigationlink.ToNode);


            }

            return true;

        }


    }






    public class GraphNode
    {
        public string Slug { get; set; }
        public List<GraphLink> NextNavigations { get; set; } = new List<GraphLink>();
    }


    public class GraphLink
    {
        public bool IsVisited { get; set; }
        public GraphNode FromNode { get; set; }
        public GraphNode ToNode { get; set; }
        public long NumberOfUsers { get; set; }
    }
}