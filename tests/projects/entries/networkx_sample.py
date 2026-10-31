# 图算法：networkx 的增删边、最短路与度序列。
import networkx as nx

graph = nx.Graph()
graph.add_edge("a", "b", weight=2)
graph.add_edge("b", "c", weight=3)
print(sorted(graph.nodes()), sorted(graph.edges(data=True)))
print(nx.shortest_path(graph, "a", "c"))
print([degree for _, degree in sorted(graph.degree())])
